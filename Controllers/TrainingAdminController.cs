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
    [Route("Admin")]
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

            // File validation
            string? sertifikatUrl = null;
            if (model.CertificateFile != null && model.CertificateFile.Length > 0)
            {
                var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
                var ext = Path.GetExtension(model.CertificateFile.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(ext))
                {
                    ModelState.AddModelError("CertificateFile", "Hanya file PDF, JPG, dan PNG yang diperbolehkan.");
                }
                if (model.CertificateFile.Length > 10 * 1024 * 1024)
                {
                    ModelState.AddModelError("CertificateFile", "Ukuran file maksimal 10MB.");
                }
            }

            // Per-worker file validation (all-or-nothing)
            if (hasWorkerCerts)
            {
                var allowedExts = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
                foreach (var wc in model.WorkerCerts!)
                {
                    if (wc.CertificateFile != null && wc.CertificateFile.Length > 0)
                    {
                        var wcExt = Path.GetExtension(wc.CertificateFile.FileName).ToLowerInvariant();
                        if (!allowedExts.Contains(wcExt))
                            ModelState.AddModelError("", $"File untuk pekerja {wc.UserId} harus berformat PDF, JPG, atau PNG.");
                        if (wc.CertificateFile.Length > 10 * 1024 * 1024)
                            ModelState.AddModelError("", $"File untuk pekerja {wc.UserId} melebihi batas 10MB.");
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
            };
            await SetTrainingCategoryViewBag();
            return View(model);
        }

        // POST /Admin/EditTraining
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> EditTraining(EditTrainingRecordViewModel model)
        {
            // File validation
            if (model.CertificateFile != null && model.CertificateFile.Length > 0)
            {
                var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
                var ext = Path.GetExtension(model.CertificateFile.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(ext))
                {
                    TempData["Error"] = "Hanya file PDF, JPG, dan PNG yang diperbolehkan.";
                    return RedirectToAction("ManageAssessment", "AssessmentAdmin", new { tab = "training" });
                }
                if (model.CertificateFile.Length > 10 * 1024 * 1024)
                {
                    TempData["Error"] = "Ukuran file maksimal 10MB.";
                    return RedirectToAction("ManageAssessment", "AssessmentAdmin", new { tab = "training" });
                }
            }

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

            await _context.SaveChangesAsync();

            var actor = await _userManager.GetUserAsync(User);
            if (actor != null)
                await _auditLog.LogAsync(actor.Id, actor.FullName, "Update",
                    $"Training record diperbarui: {model.Judul}", model.Id, "TrainingRecord");

            TempData["Success"] = "Training record berhasil diperbarui.";
            return RedirectToAction("ManageAssessment", "AssessmentAdmin", new { tab = "training" });
        }

        // POST /Admin/DeleteTraining
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> DeleteTraining(int id)
        {
            var record = await _context.TrainingRecords.FindAsync(id);
            if (record == null) return NotFound();

            if (!string.IsNullOrEmpty(record.SertifikatUrl))
            {
                var path = Path.Combine(_env.WebRootPath, record.SertifikatUrl.TrimStart('/'));
                if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
            }

            var actor = await _userManager.GetUserAsync(User);
            _context.TrainingRecords.Remove(record);
            await _context.SaveChangesAsync();

            if (actor != null)
                await _auditLog.LogAsync(actor.Id, actor.FullName, "Delete",
                    $"Training record dihapus: {record.Judul}", record.Id, "TrainingRecord");

            TempData["Success"] = "Training record berhasil dihapus.";
            return RedirectToAction("ManageAssessment", "AssessmentAdmin", new { tab = "training" });
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
        public async Task<IActionResult> ImportTraining(IFormFile? excelFile)
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

                foreach (var row in ws.RowsUsed().Skip(1))
                {
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
                            ValidUntil = DateTime.TryParse(validUntilStr, out var vu) ? vu : (DateTime?)null,
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
