using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HcPortal.Models;
using HcPortal.Data;
using HcPortal.Services;

namespace HcPortal.Controllers
{
    [Authorize]
    [Route("Admin/[action]")]
    public abstract class AdminBaseController : Controller
    {
        protected readonly ApplicationDbContext _context;
        protected readonly UserManager<ApplicationUser> _userManager;
        protected readonly AuditLogService _auditLog;
        protected readonly IWebHostEnvironment _env;

        protected AdminBaseController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            AuditLogService auditLog,
            IWebHostEnvironment env)
        {
            _context = context;
            _userManager = userManager;
            _auditLog = auditLog;
            _env = env;
        }

        protected bool IsAjaxRequest()
            => Request.Headers["X-Requested-With"] == "XMLHttpRequest";

        protected static string MapKategori(string? raw, Dictionary<string, string>? rawToDisplayMap)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "-";
            var trimmed = raw.Trim();
            if (rawToDisplayMap != null && rawToDisplayMap.TryGetValue(trimmed.ToUpperInvariant(), out var displayName))
                return displayName;
            return trimmed;
        }

        protected async Task<List<SertifikatRow>> BuildRenewalRowsAsync()
        {
            // Query TrainingRecords with certificate (no role scoping — Admin/HC full access)
            var trainingAnon = await _context.TrainingRecords
                .AsNoTracking()
                .Include(t => t.User)
                .Where(t => t.SertifikatUrl != null)
                .Select(t => new
                {
                    t.Id,
                    UserId = t.User != null ? t.User.Id : "",
                    NamaWorker = t.User != null ? t.User.FullName : "",
                    Bagian = t.User != null ? t.User.Section : null,
                    Unit = t.User != null ? t.User.Unit : null,
                    Judul = t.Judul ?? "",
                    t.Kategori,
                    t.NomorSertifikat,
                    TanggalTerbit = (DateTime?)t.Tanggal,
                    t.ValidUntil,
                    t.CertificateType,
                    t.SertifikatUrl
                })
                .ToListAsync();

            // ===== Renewal chain resolution: batch lookup =====
            var renewedByAsSessionIds = await _context.AssessmentSessions
                .AsNoTracking()
                .Where(a => a.RenewsSessionId.HasValue && a.IsPassed == true)
                .Select(a => a.RenewsSessionId!.Value)
                .Distinct()
                .ToListAsync();

            var renewedByTrSessionIds = await _context.TrainingRecords
                .AsNoTracking()
                .Where(t => t.RenewsSessionId.HasValue)
                .Select(t => t.RenewsSessionId!.Value)
                .Distinct()
                .ToListAsync();

            var renewedByAsTrainingIds = await _context.AssessmentSessions
                .AsNoTracking()
                .Where(a => a.RenewsTrainingId.HasValue && a.IsPassed == true)
                .Select(a => a.RenewsTrainingId!.Value)
                .Distinct()
                .ToListAsync();

            var renewedByTrTrainingIds = await _context.TrainingRecords
                .AsNoTracking()
                .Where(t => t.RenewsTrainingId.HasValue)
                .Select(t => t.RenewsTrainingId!.Value)
                .Distinct()
                .ToListAsync();

            var renewedAssessmentSessionIds = new HashSet<int>(renewedByAsSessionIds);
            renewedAssessmentSessionIds.UnionWith(renewedByTrSessionIds);

            var renewedTrainingRecordIds = new HashSet<int>(renewedByAsTrainingIds);
            renewedTrainingRecordIds.UnionWith(renewedByTrTrainingIds);

            // Build rawToDisplayMap from AssessmentCategories for MapKategori DB lookup
            var allCategories = await _context.AssessmentCategories
                .AsNoTracking()
                .Where(c => c.IsActive)
                .Select(c => new { c.Id, c.Name, c.ParentId })
                .ToListAsync();
            var rawToDisplayMap = allCategories
                .Where(c => c.ParentId == null)
                .GroupBy(c => c.Name.ToUpperInvariant())
                .ToDictionary(g => g.Key, g => g.First().Name);
            if (!rawToDisplayMap.ContainsKey("MANDATORY"))
                rawToDisplayMap["MANDATORY"] = "Mandatory HSSE Training";
            if (!rawToDisplayMap.ContainsKey("PROTON"))
                rawToDisplayMap["PROTON"] = "Assessment Proton";

            var trainingRows = trainingAnon.Select(t => new SertifikatRow
            {
                SourceId = t.Id,
                RecordType = RecordType.Training,
                WorkerId = t.UserId,
                NamaWorker = t.NamaWorker,
                Bagian = t.Bagian,
                Unit = t.Unit,
                Judul = t.Judul,
                Kategori = MapKategori(t.Kategori, rawToDisplayMap),
                SubKategori = null,
                NomorSertifikat = t.NomorSertifikat,
                TanggalTerbit = t.TanggalTerbit,
                ValidUntil = t.ValidUntil,
                Status = SertifikatRow.DeriveCertificateStatus(t.ValidUntil, t.CertificateType),
                SertifikatUrl = t.SertifikatUrl,
                IsRenewed = renewedTrainingRecordIds.Contains(t.Id)
            }).ToList();

            // Query AssessmentSessions with certificate
            var categoryById = allCategories.ToDictionary(c => c.Id);
            var categoryNameLookup = allCategories
                .Where(c => c.ParentId != null && categoryById.ContainsKey(c.ParentId.Value))
                .GroupBy(c => c.Name)
                .ToDictionary(g => g.Key, g => categoryById[g.First().ParentId!.Value].Name);

            var assessmentAnon = await _context.AssessmentSessions
                .AsNoTracking()
                .Include(a => a.User)
                .Where(a => a.GenerateCertificate && a.IsPassed == true)
                .Select(a => new
                {
                    a.Id,
                    a.UserId,
                    NamaWorker = a.User != null ? a.User.FullName : "",
                    Bagian = a.User != null ? a.User.Section : null,
                    Unit = a.User != null ? a.User.Unit : null,
                    a.Title,
                    a.Category,
                    a.NomorSertifikat,
                    a.CompletedAt,
                    a.ValidUntil
                })
                .ToListAsync();

            var assessmentRows = assessmentAnon.Select(a =>
            {
                string kategori = a.Category;
                string? subKategori = null;
                if (categoryNameLookup.TryGetValue(a.Category, out var parentName))
                {
                    kategori = parentName;
                    subKategori = a.Category;
                }
                return new SertifikatRow
                {
                    SourceId = a.Id,
                    RecordType = RecordType.Assessment,
                    WorkerId = a.UserId,
                    NamaWorker = a.NamaWorker,
                    Bagian = a.Bagian,
                    Unit = a.Unit,
                    Judul = a.Title,
                    Kategori = kategori,
                    SubKategori = subKategori,
                    NomorSertifikat = a.NomorSertifikat,
                    TanggalTerbit = a.CompletedAt,
                    ValidUntil = a.ValidUntil,
                    Status = SertifikatRow.DeriveCertificateStatus(a.ValidUntil, null),
                    SertifikatUrl = null,
                    IsRenewed = renewedAssessmentSessionIds.Contains(a.Id)
                };
            }).ToList();

            // Merge all rows
            var rows = new List<SertifikatRow>(trainingRows.Count + assessmentRows.Count);
            rows.AddRange(trainingRows);
            rows.AddRange(assessmentRows);

            // POST-FILTER: hanya Expired/AkanExpired yang belum di-renew
            rows = rows
                .Where(r => !r.IsRenewed && (r.Status == CertificateStatus.Expired || r.Status == CertificateStatus.AkanExpired))
                .OrderBy(r => r.Status == CertificateStatus.Expired ? 0 : 1)
                .ThenBy(r => r.ValidUntil ?? DateOnly.MaxValue)
                .ToList();

            return rows;
        }

        // ============================================================
        // Phase 367: shared cascade-delete endpoint helpers (tab-1 AssessmentAdmin + tab-2 TrainingAdmin)
        // ============================================================

        // Image SOAL (Question+Option ImagePath) Distinct utk daftar session node cascade (Opsi B — engine TIDAK
        // sentuh image SOAL; endpoint yang bersihkan, termasuk turunan renewal). Single-source: 3 endpoint tab-1 + tab-2.
        protected async Task<List<string>> CollectQuestionImagePathsAsync(IReadOnlyCollection<int> sessionIds)
        {
            if (sessionIds.Count == 0) return new List<string>();
            var packages = await _context.AssessmentPackages
                .Include(p => p.Questions).ThenInclude(q => q.Options)
                .Where(p => sessionIds.Contains(p.AssessmentSessionId))
                .ToListAsync();
            return packages
                .SelectMany(p => p.Questions)
                .SelectMany(q => new[] { q.ImagePath }.Concat(q.Options.Select(o => o.ImagePath)))
                .Where(p => !string.IsNullOrEmpty(p))
                .Select(p => p!)
                .Distinct()
                .ToList();
        }

        // #19/L-08: hapus file sertifikat manual fisik POST-commit, warn-only per file (confined webroot V12).
        // TANPA ref-check DB — caller WAJIB scope ke node yg BENAR-BENAR ter-commit (cegah hapus cert sesi surviving).
        protected void DeleteCertFiles(IEnumerable<string> certUrls, ILogger logger)
        {
            foreach (var url in certUrls.Where(u => !string.IsNullOrEmpty(u)).Distinct())
            {
                try
                {
                    var path = System.IO.Path.Combine(_env.WebRootPath, url.TrimStart('/').Replace('/', System.IO.Path.DirectorySeparatorChar));
                    if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
                }
                catch (Exception ex) { logger.LogWarning(ex, "Cert File.Delete post-commit failed: {Url}", url); }
            }
        }

        // D-19 (Phase 312/367): sesi Pre/Post (pasangan gain-score via LinkedSessionId) TAK boleh dihapus SATUAN
        // (orphan pasangan) — arahkan ke hapus grup Pre-Post. Single-source: tab-1 DeleteAssessment + tab-2
        // DeleteManualAssessment generik. Engine #8 hanya null-clear pasangan, BUKAN cascade pasangan.
        public static bool IsPrePostSession(AssessmentSession session)
            => session.AssessmentType == "PreTest" || session.AssessmentType == "PostTest";

        // Phase 367 (06): predikat HC-tier — true bila ADA node cascade (session) Status=="Completed" ATAU ber-jawaban
        // peserta. Single-source proteksi data peserta atas FULL cascade set: tab-1 EnsureCanDeleteAsync (entity list,
        // semantik identik) + tab-2 DeleteTraining/DeleteManualAssessment (id set). HC diblok bila true; Admin override di caller.
        protected async Task<bool> CascadeHasCompletedOrAnsweredAsync(IReadOnlyCollection<int> cascadeSessionIds)
        {
            if (cascadeSessionIds.Count == 0) return false;
            if (await _context.AssessmentSessions.AnyAsync(s => cascadeSessionIds.Contains(s.Id) && s.Status == "Completed"))
                return true;
            return await _context.PackageUserResponses.AnyAsync(r => cascadeSessionIds.Contains(r.AssessmentSessionId));
        }
    }
}
