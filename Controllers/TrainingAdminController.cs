using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using HcPortal.Models;
using HcPortal.Data;
using HcPortal.Services;
using ClosedXML.Excel;
using System.Globalization;
using HcPortal.Helpers;

namespace HcPortal.Controllers
{
    [Route("Admin/[action]")]
    public class TrainingAdminController : AdminBaseController
    {
        private readonly ILogger<TrainingAdminController> _logger;

        public TrainingAdminController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            AuditLogService auditLog,
            IWebHostEnvironment env,
            ILogger<TrainingAdminController> logger)
            : base(context, userManager, auditLog, env)
        {
            _logger = logger;
        }

        // Override View resolution to use Views/Admin/ folder
        protected new ViewResult View() => base.View("~/Views/Admin/" + ControllerContext.ActionDescriptor.ActionName + ".cshtml");
        protected new ViewResult View(object? model) => base.View("~/Views/Admin/" + ControllerContext.ActionDescriptor.ActionName + ".cshtml", model);
        protected new ViewResult View(string viewName) => base.View(viewName.StartsWith("~/") ? viewName : "~/Views/Admin/" + viewName + ".cshtml");
        protected new ViewResult View(string viewName, object? model) => base.View(viewName.StartsWith("~/") ? viewName : "~/Views/Admin/" + viewName + ".cshtml", model);

        private async Task SetTrainingCategoryViewBag()
        {
            var allCats = await _context.AssessmentCategories
                .Where(c => c.ParentId == null && c.IsActive)
                .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
                .Select(c => new { c.Id, c.Name })
                .ToListAsync();
            ViewBag.KategoriOptions = allCats
                .GroupBy(c => c.Name)
                .Select(g => g.First())
                .ToList();

            ViewBag.SubKategoriOptions = await _context.AssessmentCategories
                .Where(c => c.ParentId != null && c.IsActive)
                .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
                .Select(c => new { c.Id, c.Name, c.ParentId })
                .ToListAsync();
        }

        private async Task PopulateWorkersViewBag()
        {
            var workers = await _context.Users
                .OrderBy(u => u.FullName)
                .Select(u => new { u.Id, u.FullName, u.NIP })
                .ToListAsync();
            ViewBag.Workers = workers.Select(w => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
            {
                Value = w.Id,
                Text = $"{w.FullName} ({w.NIP ?? "No NIP"})"
            }).ToList();
        }

        // GET /Admin/AddTraining
        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> AddTraining(
            [FromQuery] List<int>? renewSessionId = null,
            [FromQuery] List<int>? renewTrainingId = null)
        {
            var workers = await _context.Users
                .OrderBy(u => u.FullName)
                .Select(u => new { u.Id, u.FullName, u.NIP })
                .ToListAsync();

            ViewBag.Workers = workers.Select(w => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
            {
                Value = w.Id,
                Text = $"{w.FullName} ({w.NIP ?? "No NIP"})"
            }).ToList();

            bool isRenewalMode = false;
            var model = new CreateTrainingRecordViewModel();

            if (renewTrainingId != null && renewTrainingId.Count > 0)
            {
                if (renewTrainingId.Count == 1)
                {
                    var src = await _context.TrainingRecords.Include(t => t.User)
                        .FirstOrDefaultAsync(t => t.Id == renewTrainingId[0]);
                    if (src == null)
                    {
                        TempData["Warning"] = "Training record asal tidak ditemukan.";
                        return RedirectToAction("AddTraining");
                    }
                    isRenewalMode = true;
                    model.Judul = src.Judul ?? "";
                    model.Kategori = src.Kategori ?? "";
                    model.RenewsTrainingId = src.Id;
                    ViewBag.RenewalSourceTitle = src.Judul ?? "";
                    ViewBag.RenewalSourceUserName = src.User?.FullName ?? "";
                    ViewBag.SelectedUserId = src.UserId;
                }
                else
                {
                    var srcs = await _context.TrainingRecords.Include(t => t.User)
                        .Where(t => renewTrainingId.Contains(t.Id)).ToListAsync();
                    if (srcs.Count == 0)
                    {
                        TempData["Warning"] = "Training record asal tidak ditemukan.";
                        return RedirectToAction("AddTraining");
                    }
                    isRenewalMode = true;
                    var first = srcs[0];
                    model.Judul = first.Judul ?? "";
                    model.Kategori = first.Kategori ?? "";
                    ViewBag.RenewalSourceTitle = first.Judul ?? "";
                    ViewBag.RenewalSourceUserName = string.Join(", ", srcs.Select(t => t.User?.FullName ?? ""));
                    ViewBag.RenewalFkMap = System.Text.Json.JsonSerializer.Serialize(
                        srcs.ToDictionary(t => t.UserId ?? "", t => t.Id));
                    ViewBag.RenewalFkMapType = "training";
                    ViewBag.SelectedUserIds = srcs.Select(t => t.UserId).ToList();
                }
            }
            else if (renewSessionId != null && renewSessionId.Count > 0)
            {
                // Build category lookup for MapKategori DB lookup (LDAT-05)
                var catsForSessionRenewal = (await _context.AssessmentCategories
                    .Where(c => c.IsActive && c.ParentId == null)
                    .ToListAsync())
                    .GroupBy(c => c.Name.ToUpperInvariant())
                    .ToDictionary(g => g.Key, g => g.First().Name);
                if (!catsForSessionRenewal.ContainsKey("MANDATORY")) catsForSessionRenewal["MANDATORY"] = "Mandatory HSSE Training";
                if (!catsForSessionRenewal.ContainsKey("PROTON")) catsForSessionRenewal["PROTON"] = "Assessment Proton";

                if (renewSessionId.Count == 1)
                {
                    var src = await _context.AssessmentSessions.Include(s => s.User)
                        .FirstOrDefaultAsync(s => s.Id == renewSessionId[0]);
                    if (src == null)
                    {
                        TempData["Warning"] = "Assessment session asal tidak ditemukan.";
                        return RedirectToAction("AddTraining");
                    }
                    isRenewalMode = true;
                    model.Judul = src.Title ?? "";
                    model.Kategori = MapKategori(src.Category, catsForSessionRenewal);
                    model.RenewsSessionId = src.Id;
                    ViewBag.RenewalSourceTitle = src.Title ?? "";
                    ViewBag.RenewalSourceUserName = src.User?.FullName ?? "";
                    ViewBag.SelectedUserId = src.UserId;
                }
                else
                {
                    var srcs = await _context.AssessmentSessions.Include(s => s.User)
                        .Where(s => renewSessionId.Contains(s.Id)).ToListAsync();
                    if (srcs.Count == 0)
                    {
                        TempData["Warning"] = "Assessment session asal tidak ditemukan.";
                        return RedirectToAction("AddTraining");
                    }
                    isRenewalMode = true;
                    var first = srcs[0];
                    model.Judul = first.Title ?? "";
                    model.Kategori = MapKategori(first.Category, catsForSessionRenewal);
                    ViewBag.RenewalSourceTitle = first.Title ?? "";
                    ViewBag.RenewalSourceUserName = string.Join(", ", srcs.Select(s => s.User?.FullName ?? ""));
                    ViewBag.RenewalFkMap = System.Text.Json.JsonSerializer.Serialize(
                        srcs.ToDictionary(s => s.UserId, s => s.Id));
                    ViewBag.RenewalFkMapType = "session";
                    ViewBag.SelectedUserIds = srcs.Select(s => s.UserId).ToList();
                }
            }

            ViewBag.IsRenewalMode = isRenewalMode;
            await SetTrainingCategoryViewBag();
            return View(model);
        }

        // POST /Admin/AddTraining
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> AddTraining(CreateTrainingRecordViewModel model)
        {
            // Manual validation: minimal 1 pekerja harus dipilih
            bool hasWorkerCerts = model.WorkerCerts != null && model.WorkerCerts.Count > 0;
            bool hasSingleUser = !string.IsNullOrEmpty(model.UserId);
            if (!hasWorkerCerts && !hasSingleUser)
            {
                ModelState.AddModelError("", "Pilih minimal 1 pekerja.");
            }
            if (hasWorkerCerts && model.WorkerCerts!.Count > 20)
            {
                ModelState.AddModelError("", "Maksimal 20 pekerja per submission.");
            }

            // Phase 325: pakai FileUploadHelper.ValidateCertificateFile (extension + size + magic byte D-02/D-09).
            // Sebelumnya inline duplicate — magic byte fix Plan 02 bypass-able tanpa refactor ini (RESEARCH §Pitfall 1).
            string? sertifikatUrl = null;
            var (certValid, certErr) = FileUploadHelper.ValidateCertificateFile(model.CertificateFile);
            if (!certValid)
                ModelState.AddModelError("CertificateFile", certErr!);

            // Phase 325: per-worker file validation pakai helper (extension + size + magic byte).
            if (hasWorkerCerts)
            {
                foreach (var wc in model.WorkerCerts!)
                {
                    var (wcValid, wcErr) = FileUploadHelper.ValidateCertificateFile(wc.CertificateFile);
                    if (!wcValid)
                    {
                        // Preserve message context "untuk pekerja {UserId}" agar UX tetap informatif siapa yang error.
                        ModelState.AddModelError("", $"File untuk pekerja {wc.UserId}: {wcErr}");
                    }
                }
            }

            // Baca bulk renewal params
            var fkMapJson = Request.Form["renewalFkMap"].FirstOrDefault();
            var fkMapType = Request.Form["renewalFkMapType"].FirstOrDefault();
            Dictionary<string, int>? fkMap = null;
            if (!string.IsNullOrEmpty(fkMapJson))
                fkMap = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, int>>(fkMapJson);

            var userIdsJson = Request.Form["UserIds"].FirstOrDefault();
            List<string>? bulkUserIds = null;
            if (!string.IsNullOrEmpty(userIdsJson))
                bulkUserIds = System.Text.Json.JsonSerializer.Deserialize<List<string>>(userIdsJson);

            // FK mutual exclusion (per D-04)
            if (model.RenewsTrainingId.HasValue && model.RenewsSessionId.HasValue)
            {
                ModelState.AddModelError("", "Renewal FK tidak valid: hanya boleh mengisi salah satu dari RenewsTrainingId atau RenewsSessionId.");
            }
            // Double renewal prevention (per D-10)
            if (model.RenewsTrainingId.HasValue)
            {
                var srcAlreadyRenewed = await _context.TrainingRecords.AnyAsync(t => t.RenewsTrainingId == model.RenewsTrainingId)
                    || await _context.AssessmentSessions.AnyAsync(a => a.RenewsTrainingId == model.RenewsTrainingId && a.IsPassed == true);
                if (srcAlreadyRenewed)
                    ModelState.AddModelError("", "Sertifikat ini sudah di-renew sebelumnya.");
            }
            if (model.RenewsSessionId.HasValue)
            {
                var srcAlreadyRenewed = await _context.TrainingRecords.AnyAsync(t => t.RenewsSessionId == model.RenewsSessionId)
                    || await _context.AssessmentSessions.AnyAsync(a => a.RenewsSessionId == model.RenewsSessionId && a.IsPassed == true);
                if (srcAlreadyRenewed)
                    ModelState.AddModelError("", "Sertifikat ini sudah di-renew sebelumnya.");
            }
            // P03 (Phase 326 D-01/D-08/D-10): DAG enforcement — tanggal renewal harus > tanggal source (monotonic constraint, strict reject same-day)
            if (model.RenewsTrainingId.HasValue)
            {
                var src = await _context.TrainingRecords.FindAsync(model.RenewsTrainingId.Value);
                if (src != null && src.Tanggal >= model.Tanggal)
                    ModelState.AddModelError("", "Tanggal renewal harus lebih besar dari tanggal sertifikat yang di-renew.");
            }
            if (model.RenewsSessionId.HasValue)
            {
                var srcAs = await _context.AssessmentSessions.FindAsync(model.RenewsSessionId.Value);
                if (srcAs != null && srcAs.Schedule >= model.Tanggal)
                    ModelState.AddModelError("", "Tanggal renewal harus lebih besar dari tanggal sertifikat yang di-renew.");
            }
            // P06 (Phase 326 D-02/D-03/D-05): Permanent + ValidUntil mutual exclusion (field-level error key=ValidUntil)
            if (model.CertificateType == "Permanent" && model.ValidUntil != null)
                ModelState.AddModelError("ValidUntil", "Sertifikat Permanent tidak boleh punya tanggal expired.");
            // Mixed-type bulk validation (per D-11, EDGE-01)
            if (fkMap != null && bulkUserIds != null && bulkUserIds.Count > 1)
            {
                if (string.IsNullOrEmpty(fkMapType) || (fkMapType != "training" && fkMapType != "session"))
                {
                    ModelState.AddModelError("", "Bulk renewal tidak dapat mencampur tipe Assessment dan Training. Renew per tipe secara terpisah.");
                }
            }

            if (!ModelState.IsValid)
            {
                var workersList = await _context.Users
                    .OrderBy(u => u.FullName)
                    .Select(u => new { u.Id, u.FullName, u.NIP })
                    .ToListAsync();
                ViewBag.Workers = workersList.Select(w => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = w.Id,
                    Text = $"{w.FullName} ({w.NIP ?? "No NIP"})"
                }).ToList();
                ViewBag.IsRenewalMode = (model.RenewsTrainingId != null || model.RenewsSessionId != null || !string.IsNullOrEmpty(fkMapJson));
                if (model.RenewsTrainingId != null)
                {
                    var src = await _context.TrainingRecords.Include(t => t.User).FirstOrDefaultAsync(t => t.Id == model.RenewsTrainingId);
                    if (src != null) { ViewBag.RenewalSourceTitle = src.Judul ?? ""; ViewBag.RenewalSourceUserName = src.User?.FullName ?? ""; }
                }
                else if (model.RenewsSessionId != null)
                {
                    var src = await _context.AssessmentSessions.Include(s => s.User).FirstOrDefaultAsync(s => s.Id == model.RenewsSessionId);
                    if (src != null) { ViewBag.RenewalSourceTitle = src.Title ?? ""; ViewBag.RenewalSourceUserName = src.User?.FullName ?? ""; }
                }
                await SetTrainingCategoryViewBag();
                return View(model);
            }

            // Handle file upload
            var uploadedUrl = await FileUploadHelper.SaveFileAsync(model.CertificateFile, _env.WebRootPath, "uploads/certificates");
            if (uploadedUrl != null)
            {
                sertifikatUrl = uploadedUrl;
            }

            // Multi-select non-renewal: create 1 record per worker with per-worker cert
            if (hasWorkerCerts && bulkUserIds == null)
            {
                foreach (var wc in model.WorkerCerts!)
                {
                    string? wcUrl = null;
                    if (wc.CertificateFile != null && wc.CertificateFile.Length > 0)
                        wcUrl = await FileUploadHelper.SaveFileAsync(wc.CertificateFile, _env.WebRootPath, "uploads/certificates");

                    var rec = new TrainingRecord
                    {
                        UserId = wc.UserId,
                        Judul = model.Judul,
                        Penyelenggara = model.Penyelenggara,
                        Kota = model.Kota,
                        Kategori = model.Kategori,
                        SubKategori = model.SubKategori,
                        Tanggal = model.Tanggal,
                        TanggalMulai = model.TanggalMulai,
                        TanggalSelesai = model.TanggalSelesai,
                        Status = model.Status,
                        NomorSertifikat = wc.NomorSertifikat,
                        ValidUntil = model.ValidUntil,
                        CertificateType = model.CertificateType,
                        SertifikatUrl = wcUrl
                    };
                    _context.TrainingRecords.Add(rec);
                }
                await _context.SaveChangesAsync();

                var multiActor = await _userManager.GetUserAsync(User);
                if (multiActor != null)
                    await _auditLog.LogAsync(multiActor.Id, multiActor.FullName, "Create",
                        $"Training record ditambahkan: {model.Judul} untuk {model.WorkerCerts!.Count} pekerja", 0, "TrainingRecord");

                TempData["Success"] = $"Berhasil membuat {model.WorkerCerts!.Count} training record.";
                return RedirectToAction("ManageAssessment", "AssessmentAdmin", new { tab = "training" });
            }

            // Bulk renewal: buat N TrainingRecord dengan FK per-user
            if (bulkUserIds != null && bulkUserIds.Count > 1)
            {
                foreach (var uid in bulkUserIds)
                {
                    var rec = new TrainingRecord
                    {
                        UserId = uid,
                        Judul = model.Judul,
                        Penyelenggara = model.Penyelenggara,
                        Kota = model.Kota,
                        Kategori = model.Kategori,
                        SubKategori = model.SubKategori,
                        Tanggal = model.Tanggal,
                        TanggalMulai = model.TanggalMulai,
                        TanggalSelesai = model.TanggalSelesai,
                        Status = model.Status,
                        NomorSertifikat = model.NomorSertifikat,
                        ValidUntil = model.ValidUntil,
                        CertificateType = model.CertificateType,
                        SertifikatUrl = sertifikatUrl
                    };

                    if (fkMap != null && fkMap.TryGetValue(uid, out var fkId))
                    {
                        if (fkMapType == "training") rec.RenewsTrainingId = fkId;
                        else rec.RenewsSessionId = fkId;
                    }

                    _context.TrainingRecords.Add(rec);
                }
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Berhasil membuat {bulkUserIds.Count} training record (renewal).";
                return RedirectToAction("ManageAssessment", "AssessmentAdmin", new { tab = "training" });
            }

            var record = new TrainingRecord
            {
                UserId = model.UserId,
                Judul = model.Judul,
                Penyelenggara = model.Penyelenggara,
                Kota = model.Kota,
                Kategori = model.Kategori,
                SubKategori = model.SubKategori,
                Tanggal = model.Tanggal,
                TanggalMulai = model.TanggalMulai,
                TanggalSelesai = model.TanggalSelesai,
                Status = model.Status,
                NomorSertifikat = model.NomorSertifikat,
                ValidUntil = model.ValidUntil,
                CertificateType = model.CertificateType,
                SertifikatUrl = sertifikatUrl,
                RenewsTrainingId = model.RenewsTrainingId,
                RenewsSessionId = model.RenewsSessionId
            };

            _context.TrainingRecords.Add(record);
            await _context.SaveChangesAsync();

            var actor = await _userManager.GetUserAsync(User);
            var workerName = (await _context.Users.FindAsync(model.UserId))?.FullName ?? model.UserId;
            if (actor != null)
                await _auditLog.LogAsync(actor.Id, actor.FullName, "Create",
                    $"Training record ditambahkan: {model.Judul} untuk {workerName}", record.Id, "TrainingRecord");

            TempData["Success"] = "Training record berhasil dibuat.";
            return RedirectToAction("ManageAssessment", "AssessmentAdmin", new { tab = "training" });
        }

        // GET /Admin/EditTraining/{id}
        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> EditTraining(int id)
        {
            var record = await _context.TrainingRecords
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == id);
            if (record == null) return NotFound();

            var model = new EditTrainingRecordViewModel
            {
                Id = record.Id,
                WorkerId = record.UserId,
                WorkerName = record.User?.FullName ?? "",
                Judul = record.Judul,
                Penyelenggara = record.Penyelenggara,
                Kota = record.Kota,
                Kategori = record.Kategori,
                SubKategori = record.SubKategori,
                Tanggal = record.Tanggal,
                TanggalMulai = record.TanggalMulai,
                TanggalSelesai = record.TanggalSelesai,
                Status = record.Status,
                NomorSertifikat = record.NomorSertifikat,
                ValidUntil = record.ValidUntil,
                CertificateType = record.CertificateType,
                ExistingSertifikatUrl = record.SertifikatUrl,
                RenewsTrainingId = record.RenewsTrainingId,
                RenewsSessionId = record.RenewsSessionId,
            };
            // Phase 326 D-07: Populate RenewalSourceTitle untuk display read-only di EditTraining view
            if (model.RenewsTrainingId != null)
            {
                var src = await _context.TrainingRecords.FirstOrDefaultAsync(t => t.Id == model.RenewsTrainingId);
                model.RenewalSourceTitle = src?.Judul ?? "(sertifikat sumber tidak ditemukan)";
            }
            else if (model.RenewsSessionId != null)
            {
                var srcAs = await _context.AssessmentSessions.FirstOrDefaultAsync(s => s.Id == model.RenewsSessionId);
                model.RenewalSourceTitle = srcAs?.Title ?? "(sertifikat sumber tidak ditemukan)";
            }
            await SetTrainingCategoryViewBag();
            return View(model);
        }

        // POST /Admin/EditTraining
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> EditTraining(EditTrainingRecordViewModel model)
        {
            // Phase 325: pakai FileUploadHelper.ValidateCertificateFile (extension + size + magic byte D-02/D-09).
            // Pattern endpoint ini: TempData + RedirectToAction (bukan ModelState) karena dipanggil dari ManageAssessment redirect-back.
            var (editValid, editErr) = FileUploadHelper.ValidateCertificateFile(model.CertificateFile);
            if (!editValid)
            {
                TempData["Error"] = editErr;
                return RedirectToAction("ManageAssessment", "AssessmentAdmin", new { tab = "training" });
            }

            // P03 (Phase 326 D-01/D-08/D-10): DAG enforcement — mirror AddTraining
            if (model.RenewsTrainingId.HasValue)
            {
                var src = await _context.TrainingRecords.FindAsync(model.RenewsTrainingId.Value);
                if (src != null && src.Tanggal >= model.Tanggal)
                    ModelState.AddModelError("", "Tanggal renewal harus lebih besar dari tanggal sertifikat yang di-renew.");
            }
            if (model.RenewsSessionId.HasValue)
            {
                var srcAs = await _context.AssessmentSessions.FindAsync(model.RenewsSessionId.Value);
                if (srcAs != null && srcAs.Schedule >= model.Tanggal)
                    ModelState.AddModelError("", "Tanggal renewal harus lebih besar dari tanggal sertifikat yang di-renew.");
            }
            // P03 Self-renewal guard (Phase 326 D-07 defense kalau form tampering set RenewsTrainingId=model.Id)
            if (model.RenewsTrainingId.HasValue && model.RenewsTrainingId.Value == model.Id)
                ModelState.AddModelError("", "Sertifikat tidak boleh renewal dirinya sendiri.");
            // P06 (Phase 326 D-02/D-03): Permanent + ValidUntil mutual exclusion
            if (model.CertificateType == "Permanent" && model.ValidUntil != null)
                ModelState.AddModelError("ValidUntil", "Sertifikat Permanent tidak boleh punya tanggal expired.");

            if (!ModelState.IsValid)
            {
                var firstError = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .FirstOrDefault() ?? "Data tidak valid.";
                TempData["Error"] = firstError;
                return RedirectToAction("ManageAssessment", "AssessmentAdmin", new { tab = "training" });
            }

            var record = await _context.TrainingRecords.FindAsync(model.Id);
            if (record == null) return NotFound();

            // Handle file upload — replace old file if new file provided
            if (model.CertificateFile != null && model.CertificateFile.Length > 0)
            {
                if (!string.IsNullOrEmpty(record.SertifikatUrl))
                {
                    var oldPath = Path.Combine(_env.WebRootPath, record.SertifikatUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                }

                var uploadedUrl = await FileUploadHelper.SaveFileAsync(model.CertificateFile, _env.WebRootPath, "uploads/certificates");
                if (uploadedUrl != null) record.SertifikatUrl = uploadedUrl;
            }

            record.Judul = model.Judul;
            record.Penyelenggara = model.Penyelenggara;
            record.Kota = model.Kota;
            record.Kategori = model.Kategori;
            record.SubKategori = model.SubKategori;
            record.Tanggal = model.Tanggal;
            record.TanggalMulai = model.TanggalMulai;
            record.TanggalSelesai = model.TanggalSelesai;
            record.Status = model.Status;
            record.NomorSertifikat = model.NomorSertifikat;
            record.ValidUntil = model.ValidUntil;
            record.CertificateType = model.CertificateType;
            // Phase 326 D-07: Persist renewal FK clear/passthrough (else "Hapus link renewal" button silent ignore)
            record.RenewsTrainingId = model.RenewsTrainingId;
            record.RenewsSessionId = model.RenewsSessionId;

            await _context.SaveChangesAsync();

            var actor = await _userManager.GetUserAsync(User);
            if (actor != null)
                await _auditLog.LogAsync(actor.Id, actor.FullName, "Update",
                    $"Training record diperbarui: {model.Judul}", model.Id, "TrainingRecord");

            TempData["Success"] = "Training record berhasil diperbarui.";
            return RedirectToAction("ManageAssessment", "AssessmentAdmin", new { tab = "training" });
        }

        // POST /Admin/DeleteTraining
        // MAM-08: HTMX delete dari Tab2 Input Records. Bila request HTMX → set HX-Trigger "recordDeleted"
        // (view re-fetch Tab2 dgn hx-include filterFormTraining → filter + isFiltered=true preserved,
        // isInitialState tetap false). Non-HTMX → full-page redirect (perilaku lama).
        private bool IsHtmxRequest() => Request.Headers.ContainsKey("HX-Request");

        // Phase 367 (06) L-06: honest HTMX trigger — nilai static testable (pola 04 anti-drift, fix #1 sukses-palsu).
        public const string RecordDeletedTrigger = "recordDeleted";
        public static string BuildRecordDeleteFailedTrigger(string pesan)
            => System.Text.Json.JsonSerializer.Serialize(new { recordDeleteFailed = new { pesan } });

        // Sukses → HX-Trigger recordDeleted (200). Non-HTMX → redirect (perilaku lama).
        private IActionResult DeleteTabSuccess()
        {
            if (IsHtmxRequest())
            {
                Response.Headers["HX-Trigger"] = RecordDeletedTrigger;
                return new EmptyResult();
            }
            return RedirectToAction("ManageAssessment", "AssessmentAdmin", new { tab = "training" });
        }

        // Gagal → HX-Trigger recordDeleteFailed + payload pesan GENERIK (V7, no ex.Message) + HTTP 400 (L-06/#1).
        private IActionResult DeleteTabFailure(string pesan)
        {
            if (IsHtmxRequest())
            {
                // Phase 367 (08, UAT-fix): respons 200 (BUKAN 400) supaya HTMX MEN-DISPATCH event DOM
                // `recordDeleteFailed` (HTMX tak dispatch event kustom utk 4xx + header HX-Trigger tak andal
                // dibaca xhr.getResponseHeader). Sinyal jujur = event + flash MERAH (bukan HTTP status). #1.
                Response.Headers["HX-Trigger"] = BuildRecordDeleteFailedTrigger(pesan);
                return new EmptyResult();
            }
            TempData["Error"] = pesan;
            return RedirectToAction("ManageAssessment", "AssessmentAdmin", new { tab = "training" });
        }

        // mirrorTrainingIds dari form (checkbox opt-out mirror legacy #15) — divalidasi server-side milik-user di engine (V5/IDOR).
        private List<int> ParseMirrorTrainingIds()
        {
            var ids = new List<int>();
            if (!Request.HasFormContentType) return ids;
            foreach (var v in Request.Form["mirrorTrainingIds"])
                if (int.TryParse(v, out var n)) ids.Add(n);
            return ids;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> DeleteTraining(int id)
        {
            var cascade = HttpContext.RequestServices.GetRequiredService<RecordCascadeDeleteService>();
            var record = await _context.TrainingRecords.FindAsync(id);
            if (record == null) return NotFound();

            try
            {
                // Phase 367 L-03: pre-check renewal BLOKIR (fase 325/331) DIBALIK → cascade penuh via engine
                // (turunan renewal IKUT terhapus). Engine hapus DB (root training + turunan) + cert per node + audit (1-tx).
                var nodes = await cascade.CollectCascadeIds("training", id);
                var cascadeSessionIds = nodes.Where(n => n.Type == "session").Select(n => n.Id).ToList();

                // HC-tier guard (konsisten tab-1, shared CascadeHasCompletedOrAnsweredAsync): non-Admin diblok bila ADA
                // node cascade session Completed/ber-jawaban — cegah HC hapus data peserta via ancestor. Admin override.
                if (!User.IsInRole("Admin") && await CascadeHasCompletedOrAnsweredAsync(cascadeSessionIds))
                    return DeleteTabFailure("Tidak bisa menghapus: ada sesi yang sudah Completed atau berisi jawaban peserta. Hubungi Admin.");

                // Image SOAL (Opsi B) turunan session — collect SEBELUM engine (engine tak sentuh image SOAL).
                var imagePaths = await CollectQuestionImagePathsAsync(cascadeSessionIds);

                var actor = await _userManager.GetUserAsync(User);
                var result = await cascade.ExecuteAsync("training", id, ParseMirrorTrainingIds(), actor?.Id ?? "", actor?.FullName ?? "Unknown");
                if (!result.Success)
                    return DeleteTabFailure(result.ErrorMessage ?? "Gagal menghapus record. Silakan coba lagi.");

                // Image SOAL cleanup POST cascade (D-05 AnyAsync). Cert per node sudah dihapus engine.
                await ImageFileCleanup.DeleteUnreferencedAsync(_context, _env.WebRootPath, _logger, imagePaths, "DeleteTraining image");

                return DeleteTabSuccess();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogWarning(ex, "Delete failed for TrainingRecord {Id}", id);
                return DeleteTabFailure("Gagal hapus: ada constraint database yang dilanggar.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting TrainingRecord {Id}", id);
                return DeleteTabFailure("Gagal menghapus record. Silakan coba lagi.");
            }
        }

        // --- MANUAL ASSESSMENT ---

        // GET /Admin/AddManualAssessment
        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> AddManualAssessment()
        {
            await PopulateWorkersViewBag();
            await SetTrainingCategoryViewBag();
            return View(new CreateManualAssessmentViewModel());
        }

        // POST /Admin/AddManualAssessment
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> AddManualAssessment(CreateManualAssessmentViewModel model)
        {
            bool hasWorkerCerts = model.WorkerCerts != null && model.WorkerCerts.Count > 0;
            if (!hasWorkerCerts)
                ModelState.AddModelError("", "Pilih minimal 1 pekerja.");
            if (hasWorkerCerts && model.WorkerCerts!.Count > 20)
                ModelState.AddModelError("", "Maksimal 20 pekerja per submission.");

            // Per-worker file validation
            if (hasWorkerCerts)
            {
                foreach (var wc in model.WorkerCerts!)
                {
                    if (wc.CertificateFile != null && wc.CertificateFile.Length > 0)
                    {
                        var (isValid, error) = FileUploadHelper.ValidateCertificateFile(wc.CertificateFile);
                        if (!isValid)
                            ModelState.AddModelError("", $"File untuk pekerja {wc.UserId}: {error}");
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                await PopulateWorkersViewBag();
                await SetTrainingCategoryViewBag();
                return View(model);
            }

            // #12 D-02: guard duplikat EXACT (UserId+Title+CompletedAt manual, shared ManualDuplicatePredicate) per worker
            // → REJECT submit bila ada dup (re-entry tanggal beda LOLOS, Pitfall 7). Pre-loop SEBELUM simpan file/Add (no partial-save).
            foreach (var wc in model.WorkerCerts!)
            {
                if (await _context.AssessmentSessions.AnyAsync(ManualDuplicatePredicate(wc.UserId, model.Title, model.CompletedAt)))
                {
                    ModelState.AddModelError("", $"Duplikat: assessment '{model.Title}' untuk pekerja {wc.UserId} pada tanggal {model.CompletedAt:dd MMM yyyy} sudah ada.");
                    await PopulateWorkersViewBag();
                    await SetTrainingCategoryViewBag();
                    return View(model);
                }
            }

            var currentUserId = (await _userManager.GetUserAsync(User))?.Id;

            foreach (var wc in model.WorkerCerts!)
            {
                string? certUrl = null;
                if (wc.CertificateFile != null && wc.CertificateFile.Length > 0)
                    certUrl = await FileUploadHelper.SaveFileAsync(wc.CertificateFile, _env.WebRootPath, "uploads/certificates");

                var session = new AssessmentSession
                {
                    UserId = wc.UserId,
                    Title = model.Title,
                    Category = model.Category,
                    Score = model.Score,
                    PassPercentage = model.PassPercentage,
                    IsPassed = model.IsPassed,
                    CompletedAt = model.CompletedAt,
                    Schedule = model.CompletedAt,
                    ValidUntil = model.ValidUntil,
                    NomorSertifikat = wc.NomorSertifikat,
                    ManualSertifikatUrl = certUrl,
                    Penyelenggara = model.Penyelenggara,
                    Kota = model.Kota,
                    SubKategori = model.SubKategori,
                    CertificateType = model.CertificateType,
                    AssessmentType = AssessmentConstants.AssessmentType.Manual,
                    Status = AssessmentConstants.AssessmentStatus.Completed,
                    IsManualEntry = true,
                    GenerateCertificate = true,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = currentUserId
                };
                _context.AssessmentSessions.Add(session);
            }
            await _context.SaveChangesAsync();

            var actor = await _userManager.GetUserAsync(User);
            if (actor != null)
                await _auditLog.LogAsync(actor.Id, actor.FullName, "Create",
                    $"Assessment manual ditambahkan: {model.Title} untuk {model.WorkerCerts!.Count} pekerja", 0, "AssessmentSession");

            TempData["Success"] = $"Berhasil membuat {model.WorkerCerts!.Count} assessment manual.";
            return RedirectToAction("ManageAssessment", "AssessmentAdmin", new { tab = "training" });
        }

        // =====================================================================
        // Phase 338 REST-04 (D-04 Hybrid A3): Bulk backfill assessment dari Excel.
        // Use case: restore historical Cilacap PreTest 30 Mar 2026 (13 peserta)
        // yang hilang dari Dev DB akibat IT redeploy tanpa backup.
        // Mechanism: parse Excel → match NIP → insert AssessmentSession dalam
        // SATU transaction atomic + per-row audit `ManualImport-Backfill` tag.
        // =====================================================================

        // GET /Admin/BulkBackfill — form upload UI
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult BulkBackfill()
        {
            return View();
        }

        // POST /Admin/BulkBackfillAssessment — REST-04 execute
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        [RequestFormLimits(MultipartBodyLengthLimit = 10 * 1024 * 1024)]
        public async Task<IActionResult> BulkBackfillAssessment(
            IFormFile? excel,
            string title,
            string category,
            DateTime completedAt,
            int? linkedGroupId = null,
            int durationMinutes = 60,
            int passPercentage = 70,
            string auditTag = "ManualImport-Backfill")
        {
            if (excel == null || excel.Length == 0)
            {
                TempData["Error"] = "File Excel wajib diupload.";
                return RedirectToAction(nameof(BulkBackfill));
            }
            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(category))
            {
                TempData["Error"] = "Title + Category wajib diisi.";
                return RedirectToAction(nameof(BulkBackfill));
            }

            // Parse Excel: assume header row 1, data row 2+: kolom 1=NIP, kolom 2=Nama (informational), kolom 3=Score
            var rows = new List<(string NIP, int Score)>();
            try
            {
                using var stream = excel.OpenReadStream();
                using var workbook = new XLWorkbook(stream);
                var ws = workbook.Worksheets.First();
                int lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;
                for (int rowIdx = 2; rowIdx <= lastRow; rowIdx++)
                {
                    var nipCell = ws.Cell(rowIdx, 1).GetString().Trim();
                    if (string.IsNullOrEmpty(nipCell)) continue;
                    int score = 0;
                    var scoreCell = ws.Cell(rowIdx, 3);
                    if (scoreCell.TryGetValue<double>(out var d)) score = (int)d;
                    else if (int.TryParse(scoreCell.GetString().Trim(), out var i)) score = i;
                    rows.Add((nipCell, score));
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Gagal parse Excel: {ex.Message}";
                return RedirectToAction(nameof(BulkBackfill));
            }

            if (rows.Count == 0)
            {
                TempData["Error"] = "Tidak ada data terbaca dari Excel (kolom 1=NIP, kolom 3=Score, mulai row 2).";
                return RedirectToAction(nameof(BulkBackfill));
            }

            // Lookup UserId per NIP
            var nips = rows.Select(r => r.NIP).Distinct().ToList();
            var users = await _context.Users
                .Where(u => u.NIP != null && nips.Contains(u.NIP))
                .ToDictionaryAsync(u => u.NIP!);

            var missing = nips.Where(n => !users.ContainsKey(n)).ToList();
            if (missing.Any())
            {
                TempData["Error"] = $"NIP tidak ditemukan di AspNetUsers: {string.Join(", ", missing)}. Insert worker dulu via Admin atau correct NIP di Excel.";
                return RedirectToAction(nameof(BulkBackfill));
            }

            var actor = await _userManager.GetUserAsync(User);
            if (actor == null) return Forbid();

            int success = 0;
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var addedSessions = new List<AssessmentSession>();

                // #14 D-02: pre-load existing keys (EXACT manual; title+completedAt KONSTAN utk batch ini → key = UserId)
                // + intra-batch dedup (2 baris NIP sama dalam 1 file → baris kedua skip). seenInBatch karena AnyAsync TAK
                // lihat row uncommitted dalam tx (1 SaveChanges di akhir). EXACT: CompletedAt == (BUKAN ±1 hari, Pitfall 7).
                var relevantUserIds = users.Values.Select(u => u.Id).ToList();
                var existingUserIds = (await _context.AssessmentSessions
                    .Where(s => s.IsManualEntry && s.Title == title && s.CompletedAt == completedAt && relevantUserIds.Contains(s.UserId))
                    .Select(s => s.UserId)
                    .ToListAsync()).ToHashSet();
                var seenInBatch = new HashSet<string>();
                var skippedNips = new List<string>();

                foreach (var row in rows)
                {
                    var user = users[row.NIP];
                    // duplikat — dilewati (DB existing ATAU intra-batch); JANGAN increment success
                    if (existingUserIds.Contains(user.Id) || !seenInBatch.Add(user.Id))
                    {
                        skippedNips.Add(row.NIP);
                        continue;
                    }
                    var session = new AssessmentSession
                    {
                        UserId = user.Id,
                        Title = title,
                        Category = category,
                        Schedule = completedAt.AddHours(-1),
                        DurationMinutes = durationMinutes,
                        Status = "Completed",
                        Progress = 100,
                        BannerColor = "bg-secondary",
                        Score = row.Score,
                        IsPassed = row.Score >= passPercentage,
                        IsTokenRequired = false,
                        AccessToken = "BACKFILL",
                        PassPercentage = passPercentage,
                        CompletedAt = completedAt,
                        IsManualEntry = true,
                        LinkedGroupId = linkedGroupId,
                        AssessmentType = "Standard",
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.AssessmentSessions.Add(session);
                    addedSessions.Add(session);
                    success++;
                }
                await _context.SaveChangesAsync();

                // Per-row audit (in-tx) — TIDAK pakai _auditLog.LogAsync karena dia SaveChanges internal
                foreach (var s in addedSessions)
                {
                    _context.AuditLogs.Add(new AuditLog
                    {
                        ActorUserId = actor.Id,
                        ActorName = actor.FullName ?? actor.UserName ?? actor.Id,
                        ActionType = auditTag.Length > 50 ? auditTag.Substring(0, 50) : auditTag,
                        Description = $"BulkBackfill insert AssessmentSession Id={s.Id} UserId={s.UserId} NIP={users.First(kv => kv.Value.Id == s.UserId).Key} Score={s.Score} CompletedAt={s.CompletedAt:yyyy-MM-dd} LinkedGroupId={s.LinkedGroupId?.ToString() ?? "null"}",
                        TargetId = s.Id,
                        TargetType = "AssessmentSession",
                        CreatedAt = DateTime.UtcNow
                    });
                }
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["Success"] = $"Berhasil insert {success} backfill assessment '{title}' dengan tag '{auditTag}'. LinkedGroupId={linkedGroupId?.ToString() ?? "null"}."
                    + (skippedNips.Count > 0 ? $" {skippedNips.Count} duplikat dilewati: {string.Join(", ", skippedNips)}." : "");
                return RedirectToAction(nameof(BulkBackfill));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "BulkBackfillAssessment failed for title={Title} category={Category}", title, category);
                TempData["Error"] = $"Transaction rollback — bulk backfill gagal: {ex.Message}";
                return RedirectToAction(nameof(BulkBackfill));
            }
        }

        // GET /Admin/EditManualAssessment/{id}
        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> EditManualAssessment(int id)
        {
            var session = await _context.AssessmentSessions
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == id && s.IsManualEntry);
            if (session == null) return NotFound();

            var model = new EditManualAssessmentViewModel
            {
                Id = session.Id,
                WorkerId = session.UserId,
                WorkerName = session.User?.FullName ?? "",
                Title = session.Title,
                Category = session.Category,
                Score = session.Score,
                PassPercentage = session.PassPercentage,
                IsPassed = session.IsPassed == true,
                CompletedAt = session.CompletedAt ?? session.Schedule,
                ValidUntil = session.ValidUntil,
                NomorSertifikat = session.NomorSertifikat,
                Penyelenggara = session.Penyelenggara,
                Kota = session.Kota,
                SubKategori = session.SubKategori,
                CertificateType = session.CertificateType,
                ExistingSertifikatUrl = session.ManualSertifikatUrl
            };
            await SetTrainingCategoryViewBag();
            return View(model);
        }

        // POST /Admin/EditManualAssessment
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> EditManualAssessment(EditManualAssessmentViewModel model)
        {
            if (model.CertificateFile != null && model.CertificateFile.Length > 0)
            {
                var (isValid, error) = FileUploadHelper.ValidateCertificateFile(model.CertificateFile);
                if (!isValid)
                {
                    TempData["Error"] = error;
                    return RedirectToAction("ManageAssessment", "AssessmentAdmin", new { tab = "training" });
                }
            }

            if (!ModelState.IsValid)
            {
                var firstError = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault() ?? "Data tidak valid.";
                TempData["Error"] = firstError;
                return RedirectToAction("ManageAssessment", "AssessmentAdmin", new { tab = "training" });
            }

            var session = await _context.AssessmentSessions.FirstOrDefaultAsync(s => s.Id == model.Id && s.IsManualEntry);
            if (session == null) return NotFound();

            if (model.CertificateFile != null && model.CertificateFile.Length > 0)
            {
                FileUploadHelper.DeleteFile(_env.WebRootPath, session.ManualSertifikatUrl);
                var uploadedUrl = await FileUploadHelper.SaveFileAsync(model.CertificateFile, _env.WebRootPath, "uploads/certificates");
                if (uploadedUrl != null) session.ManualSertifikatUrl = uploadedUrl;
            }

            session.Title = model.Title;
            session.Category = model.Category;
            session.Score = model.Score;
            session.PassPercentage = model.PassPercentage;
            session.IsPassed = model.IsPassed;
            session.CompletedAt = model.CompletedAt;
            session.Schedule = model.CompletedAt;
            session.ValidUntil = model.ValidUntil;
            session.NomorSertifikat = model.NomorSertifikat;
            session.Penyelenggara = model.Penyelenggara;
            session.Kota = model.Kota;
            session.SubKategori = model.SubKategori;
            session.CertificateType = model.CertificateType;
            session.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var actor = await _userManager.GetUserAsync(User);
            if (actor != null)
                await _auditLog.LogAsync(actor.Id, actor.FullName, "Update",
                    $"Assessment manual diperbarui: {model.Title}", model.Id, "AssessmentSession");

            TempData["Success"] = "Assessment manual berhasil diperbarui.";
            return RedirectToAction("ManageAssessment", "AssessmentAdmin", new { tab = "training" });
        }

        // POST /Admin/DeleteManualAssessment
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> DeleteManualAssessment(int id)
        {
            var cascade = HttpContext.RequestServices.GetRequiredService<RecordCascadeDeleteService>();
            // L-07: gate `&& s.IsManualEntry` DIHAPUS → endpoint generik (manual + online, #3/#4). authz preserved di atas.
            var session = await _context.AssessmentSessions.FirstOrDefaultAsync(s => s.Id == id);
            if (session == null) return NotFound();

            // D-19 (parity tab-1, shared IsPrePostSession): sesi Pre/Post tak boleh dihapus satuan (orphan pasangan) —
            // pakai hapus grup Pre-Post (tab-1). Pasca L-07 generik, filter IsManualEntry lama tak lagi jadi guard implisit.
            if (IsPrePostSession(session))
                return DeleteTabFailure("Sesi ini bagian dari grup Pre-Post Test. Gunakan hapus grup Pre-Post untuk menghapus keduanya.");

            try
            {
                // Phase 367 L-03: pre-check renewal BLOKIR (fase 325/331) DIBALIK → cascade penuh via engine
                // (turunan renewal IKUT terhapus, fix kasus Rino #3). RenewsSessionId child (Pitfall 2) ditangani engine.
                var nodes = await cascade.CollectCascadeIds("session", id);
                var cascadeSessionIds = nodes.Where(n => n.Type == "session").Select(n => n.Id).ToList();

                // HC-tier guard (konsisten tab-1, shared): non-Admin diblok bila ADA node cascade Completed/ber-jawaban. Admin override.
                if (!User.IsInRole("Admin") && await CascadeHasCompletedOrAnsweredAsync(cascadeSessionIds))
                    return DeleteTabFailure("Tidak bisa menghapus: ada sesi yang sudah Completed atau berisi jawaban peserta. Hubungi Admin.");

                // Image SOAL (Opsi B) semua session node — collect SEBELUM engine.
                var imagePaths = await CollectQuestionImagePathsAsync(cascadeSessionIds);

                var actor = await _userManager.GetUserAsync(User);
                var result = await cascade.ExecuteAsync("session", id, ParseMirrorTrainingIds(), actor?.Id ?? "", actor?.FullName ?? "Unknown");
                if (!result.Success)
                    return DeleteTabFailure(result.ErrorMessage ?? "Gagal menghapus record. Silakan coba lagi.");

                await ImageFileCleanup.DeleteUnreferencedAsync(_context, _env.WebRootPath, _logger, imagePaths, "DeleteManualAssessment image");

                return DeleteTabSuccess();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogWarning(ex, "Delete failed for AssessmentSession {Id}", id);
                return DeleteTabFailure("Gagal hapus: ada constraint database yang dilanggar.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting AssessmentSession {Id}", id);
                return DeleteTabFailure("Gagal menghapus record. Silakan coba lagi.");
            }
        }

        // GET /Admin/DeletePreview — partial modal daftar korban cascade (read-only, L-03 konfirmasi via preview).
        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> DeletePreview(string type, int id)
        {
            if (type != "training" && type != "session") return BadRequest();  // V5 whitelist
            var cascade = HttpContext.RequestServices.GetRequiredService<RecordCascadeDeleteService>();
            var nodes = await cascade.BuildPreviewAsync(type, id);  // ZERO mutasi (read-only)
            if (nodes == null || nodes.Count == 0) return NotFound();
            return PartialView("~/Views/Admin/Shared/_CascadePreviewModal.cshtml", nodes);
        }

        // --- IMPORT TRAINING ---

        // GET /Admin/DownloadImportTrainingTemplate
        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public IActionResult DownloadImportTrainingTemplate()
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Import Training");

            var headers = new[] {
                "NIP", "Judul", "Kategori", "SubKategori (opsional)",
                "Tanggal (YYYY-MM-DD)", "TanggalMulai (YYYY-MM-DD, opsional)", "TanggalSelesai (YYYY-MM-DD, opsional)",
                "Penyelenggara", "Kota (opsional)",
                "Status", "ValidUntil (YYYY-MM-DD, opsional)", "NomorSertifikat (opsional)"
            };
            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cell(1, i + 1).Value = headers[i];
                ws.Cell(1, i + 1).Style.Font.Bold = true;
                ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#16A34A");
                ws.Cell(1, i + 1).Style.Font.FontColor = XLColor.White;
            }

            // Example row
            ws.Cell(2, 1).Value = "123456";
            ws.Cell(2, 2).Value = "Pelatihan K3 Dasar";
            ws.Cell(2, 3).Value = "MANDATORY";
            ws.Cell(2, 4).Value = "";
            ws.Cell(2, 5).Value = "2024-03-15";
            ws.Cell(2, 6).Value = "2024-03-15";
            ws.Cell(2, 7).Value = "2024-03-17";
            ws.Cell(2, 8).Value = "Internal";
            ws.Cell(2, 9).Value = "Balikpapan";
            ws.Cell(2, 10).Value = "Passed";
            ws.Cell(2, 11).Value = "2027-03-15";
            ws.Cell(2, 12).Value = "CERT-001";
            for (int i = 1; i <= 12; i++)
            {
                ws.Cell(2, i).Style.Font.Italic = true;
                ws.Cell(2, i).Style.Font.FontColor = XLColor.Gray;
            }

            ws.Cell(3, 1).Value = "Kolom Kategori: PROTON / OJT / MANDATORY";
            ws.Cell(3, 1).Style.Font.Italic = true;
            ws.Cell(3, 1).Style.Font.FontColor = XLColor.DarkRed;

            ws.Cell(4, 1).Value = "Kolom Status: Passed / Valid / Expired / Failed";
            ws.Cell(4, 1).Style.Font.Italic = true;
            ws.Cell(4, 1).Style.Font.FontColor = XLColor.DarkRed;

            return ExcelExportHelper.ToFileResult(workbook, "training_import_template.xlsx", this);
        }

        // GET /Admin/DownloadImportAssessmentTemplate
        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public IActionResult DownloadImportAssessmentTemplate()
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Import Assessment");

            var headers = new[] {
                "NIP", "Judul", "Kategori", "SubKategori (opsional)",
                "Score (0-100, opsional)", "Lulus (Ya/Tidak)", "Tanggal (YYYY-MM-DD)",
                "Penyelenggara (opsional)", "Kota (opsional)",
                "ValidUntil (YYYY-MM-DD, opsional)", "NomorSertifikat (opsional)",
                "CertificateType (opsional)"
            };
            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cell(1, i + 1).Value = headers[i];
                ws.Cell(1, i + 1).Style.Font.Bold = true;
                ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#0EA5E9");
                ws.Cell(1, i + 1).Style.Font.FontColor = XLColor.White;
            }

            // Example row
            ws.Cell(2, 1).Value = "123456";
            ws.Cell(2, 2).Value = "Assessment K3 Dasar";
            ws.Cell(2, 3).Value = "MANDATORY";
            ws.Cell(2, 4).Value = "K3 Umum";
            ws.Cell(2, 5).Value = "85";
            ws.Cell(2, 6).Value = "Ya";
            ws.Cell(2, 7).Value = "2024-03-15";
            ws.Cell(2, 8).Value = "PT Safety Indonesia";
            ws.Cell(2, 9).Value = "Balikpapan";
            ws.Cell(2, 10).Value = "2027-03-15";
            ws.Cell(2, 11).Value = "CERT-A001";
            ws.Cell(2, 12).Value = "Kompetensi";
            for (int i = 1; i <= 12; i++)
            {
                ws.Cell(2, i).Style.Font.Italic = true;
                ws.Cell(2, i).Style.Font.FontColor = XLColor.Gray;
            }

            ws.Cell(3, 1).Value = "Kolom Lulus: Ya / Tidak";
            ws.Cell(3, 3).Value = "Kolom CertificateType: Kompetensi / Profesi / Pelatihan";
            ws.Cell(3, 3).Style.Font.Italic = true;
            ws.Cell(3, 3).Style.Font.FontColor = XLColor.DarkRed;
            ws.Cell(3, 1).Style.Font.Italic = true;
            ws.Cell(3, 1).Style.Font.FontColor = XLColor.DarkRed;

            return ExcelExportHelper.ToFileResult(workbook, "assessment_import_template.xlsx", this);
        }

        // GET /Admin/ImportTraining
        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public IActionResult ImportTraining()
        {
            return View(new List<HcPortal.Models.ImportTrainingResult>());
        }

        // POST /Admin/ImportTraining
        [HttpPost]
        [Authorize(Roles = "Admin, HC")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportTraining(IFormFile? excelFile, string? recordType)
        {
            if (excelFile == null || excelFile.Length == 0)
            {
                TempData["Error"] = "Pilih file Excel terlebih dahulu.";
                return View(new List<HcPortal.Models.ImportTrainingResult>());
            }

            var allowedExtensions = new[] { ".xlsx", ".xls" };
            var ext = Path.GetExtension(excelFile.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(ext))
            {
                TempData["Error"] = "Hanya file Excel (.xlsx, .xls) yang didukung.";
                return View(new List<HcPortal.Models.ImportTrainingResult>());
            }

            const long maxSize = 10 * 1024 * 1024;
            if (excelFile.Length > maxSize)
            {
                TempData["Error"] = "Ukuran file terlalu besar (maksimal 10MB).";
                return View(new List<HcPortal.Models.ImportTrainingResult>());
            }

            var results = new List<HcPortal.Models.ImportTrainingResult>();

            try
            {
                using var fileStream = excelFile.OpenReadStream();
                using var workbook = new XLWorkbook(fileStream);
                if (!workbook.Worksheets.Any())
                {
                    TempData["Error"] = "File Excel tidak memiliki worksheet.";
                    return RedirectToAction(nameof(ImportTraining));
                }
                var ws = workbook.Worksheets.First();
                bool isAssessmentImport = string.Equals(recordType, "Assessment", StringComparison.OrdinalIgnoreCase);

                foreach (var row in ws.RowsUsed().Skip(1))
                {
                    if (isAssessmentImport)
                    {
                        // Assessment columns: NIP, Judul, Kategori, SubKategori, Score, Lulus, Tanggal, Penyelenggara, Kota, ValidUntil, NomorSertifikat, CertificateType
                        var nip             = row.Cell(1).GetString().Trim();
                        var judul           = row.Cell(2).GetString().Trim();
                        var kategori        = row.Cell(3).GetString().Trim();
                        var subKategori     = row.Cell(4).GetString().Trim();
                        var scoreStr        = row.Cell(5).GetString().Trim();
                        var lulusStr        = row.Cell(6).GetString().Trim();
                        var tanggalStr      = row.Cell(7).GetString().Trim();
                        var penyelenggara   = row.Cell(8).GetString().Trim();
                        var kota            = row.Cell(9).GetString().Trim();
                        var validUntilStr   = row.Cell(10).GetString().Trim();
                        var nomorSertifikat = row.Cell(11).GetString().Trim();
                        var certificateType = row.Cell(12).GetString().Trim();

                        if (string.IsNullOrWhiteSpace(nip) && string.IsNullOrWhiteSpace(judul)) continue;

                        var result = new HcPortal.Models.ImportTrainingResult { NIP = nip, Judul = judul };

                        if (string.IsNullOrWhiteSpace(nip)) { result.Status = "Error"; result.Message = "NIP tidak boleh kosong"; results.Add(result); continue; }
                        if (string.IsNullOrWhiteSpace(judul)) { result.Status = "Error"; result.Message = "Judul tidak boleh kosong"; results.Add(result); continue; }
                        if (string.IsNullOrWhiteSpace(tanggalStr) || !DateTime.TryParse(tanggalStr, out var parsedDate))
                        { result.Status = "Error"; result.Message = "Format Tanggal tidak valid (YYYY-MM-DD)"; results.Add(result); continue; }

                        var targetUser = await _context.Users.FirstOrDefaultAsync(u => u.NIP == nip);
                        if (targetUser == null) { result.Status = "Error"; result.Message = $"NIP '{nip}' tidak ditemukan dalam sistem"; results.Add(result); continue; }

                        bool isPassed = lulusStr.Equals("Ya", StringComparison.OrdinalIgnoreCase);
                        int? score = int.TryParse(scoreStr, out var s) ? s : null;

                        // #12 D-02: guard duplikat EXACT (UserId+Title+CompletedAt manual, shared predicate) → SKIP-with-report.
                        // Branch ini SaveChanges per-row → AnyAsync sudah lihat row sebelumnya yg di-Add (intra-batch ter-cover).
                        if (await _context.AssessmentSessions.AnyAsync(ManualDuplicatePredicate(targetUser.Id, judul, parsedDate)))
                        {
                            result.Status = "Skip";
                            result.Message = "duplikat — dilewati";
                            results.Add(result);
                            continue;
                        }

                        try
                        {
                            var session = new AssessmentSession
                            {
                                UserId = targetUser.Id,
                                Title = judul,
                                Category = kategori,
                                Score = score,
                                PassPercentage = 70,
                                IsPassed = isPassed,
                                CompletedAt = parsedDate,
                                Schedule = parsedDate,
                                DurationMinutes = 60,
                                ValidUntil = DateTime.TryParse(validUntilStr, out var vu) ? DateOnly.FromDateTime(vu) : (DateOnly?)null,  // Phase 327 — Pattern D cast
                                SubKategori = string.IsNullOrWhiteSpace(subKategori) ? null : subKategori,
                                Penyelenggara = string.IsNullOrWhiteSpace(penyelenggara) ? null : penyelenggara,
                                Kota = string.IsNullOrWhiteSpace(kota) ? null : kota,
                                NomorSertifikat = string.IsNullOrWhiteSpace(nomorSertifikat) ? null : nomorSertifikat,
                                CertificateType = string.IsNullOrWhiteSpace(certificateType) ? null : certificateType,
                                Status = "Completed",
                                IsManualEntry = true,
                                GenerateCertificate = true,
                                CreatedAt = DateTime.UtcNow,
                                Progress = 0,
                                BannerColor = "bg-primary",
                                AllowAnswerReview = true,
                                ElapsedSeconds = 0,
                                IsTokenRequired = false,
                                AccessToken = "",
                                HasManualGrading = false,
                                SamePackage = false,
                                AssessmentType = ""
                            };
                            _context.AssessmentSessions.Add(session);
                            await _context.SaveChangesAsync();
                            result.Status = "Success";
                            result.Message = $"Assessment record berhasil dibuat untuk {targetUser.FullName}";
                        }
                        catch (Exception ex)
                        {
                            result.Status = "Error";
                            var innerEx = ex.InnerException?.Message ?? "";
                            result.Message = $"Gagal menyimpan: {ex.Message}. Inner: {innerEx}";
                        }
                        results.Add(result);
                    }
                    else
                    {
                    // Training columns (original)
                    var nip             = row.Cell(1).GetString().Trim();
                    var judul           = row.Cell(2).GetString().Trim();
                    var kategori        = row.Cell(3).GetString().Trim();
                    var subKategori     = row.Cell(4).GetString().Trim();
                    var tanggalStr      = row.Cell(5).GetString().Trim();
                    var tanggalMulaiStr = row.Cell(6).GetString().Trim();
                    var tanggalSelesaiStr = row.Cell(7).GetString().Trim();
                    var penyelenggara   = row.Cell(8).GetString().Trim();
                    var kota            = row.Cell(9).GetString().Trim();
                    var status          = row.Cell(10).GetString().Trim();
                    var validUntilStr   = row.Cell(11).GetString().Trim();
                    var nomorSertifikat = row.Cell(12).GetString().Trim();

                    // Skip completely blank rows
                    if (string.IsNullOrWhiteSpace(nip) && string.IsNullOrWhiteSpace(judul)) continue;

                    var result = new HcPortal.Models.ImportTrainingResult { NIP = nip, Judul = judul };

                    if (string.IsNullOrWhiteSpace(nip))
                    {
                        result.Status = "Error";
                        result.Message = "NIP tidak boleh kosong";
                        results.Add(result);
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(judul))
                    {
                        result.Status = "Error";
                        result.Message = "Judul tidak boleh kosong";
                        results.Add(result);
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(tanggalStr) || !DateTime.TryParse(tanggalStr, out var parsedDate))
                    {
                        result.Status = "Error";
                        result.Message = "Format Tanggal tidak valid (YYYY-MM-DD)";
                        results.Add(result);
                        continue;
                    }

                    var targetUser = await _context.Users.FirstOrDefaultAsync(u => u.NIP == nip);
                    if (targetUser == null)
                    {
                        result.Status = "Error";
                        result.Message = $"NIP '{nip}' tidak ditemukan dalam sistem";
                        results.Add(result);
                        continue;
                    }

                    try
                    {
                        var trainingRecord = new HcPortal.Models.TrainingRecord
                        {
                            UserId = targetUser.Id,
                            Judul = judul,
                            Kategori = kategori,
                            SubKategori = string.IsNullOrWhiteSpace(subKategori) ? null : subKategori,
                            Tanggal = parsedDate,
                            TanggalMulai = DateTime.TryParse(tanggalMulaiStr, out var tm) ? tm : (DateTime?)null,
                            TanggalSelesai = DateTime.TryParse(tanggalSelesaiStr, out var ts) ? ts : (DateTime?)null,
                            Penyelenggara = penyelenggara,
                            Kota = string.IsNullOrWhiteSpace(kota) ? null : kota,
                            Status = status,
                            ValidUntil = DateTime.TryParse(validUntilStr, out var vu) ? DateOnly.FromDateTime(vu) : (DateOnly?)null,  // Phase 327 — Pattern D cast
                            NomorSertifikat = nomorSertifikat
                        };
                        _context.TrainingRecords.Add(trainingRecord);
                        await _context.SaveChangesAsync();
                        result.Status = "Success";
                        result.Message = $"Training record berhasil dibuat untuk {targetUser.FullName}";
                    }
                    catch (Exception ex)
                    {
                        result.Status = "Error";
                        result.Message = $"Gagal menyimpan: {ex.Message}";
                    }

                    results.Add(result);
                    } // end else (training)
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read Excel import file for training");
                TempData["Error"] = $"Gagal memproses file: {ex.Message}";
                return View(new List<HcPortal.Models.ImportTrainingResult>());
            }

            return View(results);
        }
    }
}
