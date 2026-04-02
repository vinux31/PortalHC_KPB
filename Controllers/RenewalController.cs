using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using HcPortal.Models;
using HcPortal.Data;
using HcPortal.Services;
using HcPortal.Helpers;

namespace HcPortal.Controllers
{
    [Route("Admin")]
    [Route("Admin/[action]")]
    public class RenewalController : AdminBaseController
    {
        public RenewalController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            AuditLogService auditLog,
            IWebHostEnvironment env)
            : base(context, userManager, auditLog, env)
        {
        }

        // Override View resolution to use Views/Admin/ folder
        protected new ViewResult View() => base.View("~/Views/Admin/" + ControllerContext.ActionDescriptor.ActionName + ".cshtml");
        protected new ViewResult View(object? model) => base.View("~/Views/Admin/" + ControllerContext.ActionDescriptor.ActionName + ".cshtml", model);
        protected new ViewResult View(string viewName) => base.View(viewName.StartsWith("~/") ? viewName : "~/Views/Admin/" + viewName + ".cshtml");
        protected new ViewResult View(string viewName, object? model) => base.View(viewName.StartsWith("~/") ? viewName : "~/Views/Admin/" + viewName + ".cshtml", model);

        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> CertificateHistory(string workerId, string mode = "readonly")
        {
            if (string.IsNullOrEmpty(workerId))
                return BadRequest("workerId required");

            // 1. Query semua sertifikat pekerja ini
            var trainingCerts = await _context.TrainingRecords
                .Where(t => t.UserId == workerId && t.SertifikatUrl != null)
                .Select(t => new {
                    t.Id,
                    Judul = t.Judul ?? "",
                    t.Kategori,
                    t.NomorSertifikat,
                    TanggalTerbit = (DateTime?)t.Tanggal,
                    t.ValidUntil,
                    t.CertificateType,
                    t.SertifikatUrl,
                    t.RenewsSessionId,
                    t.RenewsTrainingId
                })
                .ToListAsync();

            var assessmentCerts = await _context.AssessmentSessions
                .Where(a => a.UserId == workerId && a.GenerateCertificate && a.IsPassed == true)
                .Select(a => new {
                    a.Id,
                    Judul = a.Title,
                    a.Category,
                    a.NomorSertifikat,
                    TanggalTerbit = a.CompletedAt,
                    a.ValidUntil,
                    a.RenewsSessionId,
                    a.RenewsTrainingId
                })
                .ToListAsync();

            // 2. Category resolution
            var allCategories = await _context.AssessmentCategories
                .Where(c => c.IsActive)
                .Select(c => new { c.Id, c.Name, c.ParentId })
                .ToListAsync();
            var categoryById = allCategories.ToDictionary(c => c.Id);
            var categoryNameLookup = allCategories
                .Where(c => c.ParentId != null && categoryById.ContainsKey(c.ParentId.Value))
                .GroupBy(c => c.Name)
                .ToDictionary(g => g.Key, g => categoryById[g.First().ParentId!.Value].Name);
            var rawToDisplayMapHist = allCategories
                .Where(c => c.ParentId == null)
                .GroupBy(c => c.Name.ToUpperInvariant())
                .ToDictionary(g => g.Key, g => g.First().Name);
            if (!rawToDisplayMapHist.ContainsKey("MANDATORY")) rawToDisplayMapHist["MANDATORY"] = "Mandatory HSSE Training";
            if (!rawToDisplayMapHist.ContainsKey("PROTON")) rawToDisplayMapHist["PROTON"] = "Assessment Proton";

            // 3. Renewal chain batch lookup — scoped to this worker's certs
            var mySessionIds = assessmentCerts.Select(a => a.Id).ToHashSet();
            var myTrainingIds = trainingCerts.Select(t => t.Id).ToHashSet();

            var renewedSessionIds = new HashSet<int>(
                await _context.AssessmentSessions
                    .Where(a => a.RenewsSessionId.HasValue && mySessionIds.Contains(a.RenewsSessionId.Value) && a.IsPassed == true)
                    .Select(a => a.RenewsSessionId!.Value).ToListAsync());
            renewedSessionIds.UnionWith(
                await _context.TrainingRecords
                    .Where(t => t.RenewsSessionId.HasValue && mySessionIds.Contains(t.RenewsSessionId.Value))
                    .Select(t => t.RenewsSessionId!.Value).ToListAsync());

            var renewedTrainingIds = new HashSet<int>(
                await _context.AssessmentSessions
                    .Where(a => a.RenewsTrainingId.HasValue && myTrainingIds.Contains(a.RenewsTrainingId.Value) && a.IsPassed == true)
                    .Select(a => a.RenewsTrainingId!.Value).ToListAsync());
            renewedTrainingIds.UnionWith(
                await _context.TrainingRecords
                    .Where(t => t.RenewsTrainingId.HasValue && myTrainingIds.Contains(t.RenewsTrainingId.Value))
                    .Select(t => t.RenewsTrainingId!.Value).ToListAsync());

            // 4. Build SertifikatRow list
            var rows = new List<SertifikatRow>();

            foreach (var t in trainingCerts)
            {
                rows.Add(new SertifikatRow
                {
                    SourceId = t.Id,
                    RecordType = RecordType.Training,
                    WorkerId = workerId,
                    Judul = t.Judul,
                    Kategori = MapKategori(t.Kategori, rawToDisplayMapHist),
                    SubKategori = null,
                    NomorSertifikat = t.NomorSertifikat,
                    TanggalTerbit = t.TanggalTerbit,
                    ValidUntil = t.ValidUntil,
                    Status = SertifikatRow.DeriveCertificateStatus(t.ValidUntil, t.CertificateType),
                    SertifikatUrl = t.SertifikatUrl,
                    IsRenewed = renewedTrainingIds.Contains(t.Id)
                });
            }

            foreach (var a in assessmentCerts)
            {
                string kategori = a.Category;
                string? subKategori = null;
                if (categoryNameLookup.TryGetValue(a.Category, out var parentName))
                {
                    kategori = parentName;
                    subKategori = a.Category;
                }
                rows.Add(new SertifikatRow
                {
                    SourceId = a.Id,
                    RecordType = RecordType.Assessment,
                    WorkerId = workerId,
                    Judul = a.Judul,
                    Kategori = kategori,
                    SubKategori = subKategori,
                    NomorSertifikat = a.NomorSertifikat,
                    TanggalTerbit = a.TanggalTerbit,
                    ValidUntil = a.ValidUntil,
                    Status = SertifikatRow.DeriveCertificateStatus(a.ValidUntil, null),
                    IsRenewed = renewedSessionIds.Contains(a.Id)
                });
            }

            // 5. Build renewal chain graph using Union-Find
            var parent = new Dictionary<string, string>();
            string Find(string x) {
                if (!parent.ContainsKey(x)) parent[x] = x;
                while (parent[x] != x) { parent[x] = parent[parent[x]]; x = parent[x]; }
                return x;
            }
            void Union(string a, string b) {
                var ra = Find(a); var rb = Find(b);
                if (ra != rb) parent[ra] = rb;
            }

            // Register all cert nodes
            foreach (var r in rows)
                Find(r.RecordType == RecordType.Assessment ? $"AS:{r.SourceId}" : $"TR:{r.SourceId}");

            // Build edges from renewal FKs
            foreach (var a in assessmentCerts)
            {
                var key = $"AS:{a.Id}";
                if (a.RenewsSessionId.HasValue) Union(key, $"AS:{a.RenewsSessionId.Value}");
                if (a.RenewsTrainingId.HasValue) Union(key, $"TR:{a.RenewsTrainingId.Value}");
            }
            foreach (var t in trainingCerts)
            {
                var key = $"TR:{t.Id}";
                if (t.RenewsSessionId.HasValue) Union(key, $"AS:{t.RenewsSessionId.Value}");
                if (t.RenewsTrainingId.HasValue) Union(key, $"TR:{t.RenewsTrainingId.Value}");
            }

            // Group rows by chain
            var groups = rows
                .GroupBy(r => Find(r.RecordType == RecordType.Assessment ? $"AS:{r.SourceId}" : $"TR:{r.SourceId}"))
                .Select(g =>
                {
                    var certs = g.OrderByDescending(c => c.ValidUntil ?? DateTime.MaxValue).ToList();
                    var oldest = g.OrderBy(c => c.ValidUntil ?? DateTime.MaxValue).First();
                    var chainTitle = !string.IsNullOrEmpty(oldest.SubKategori) ? oldest.SubKategori
                                   : !string.IsNullOrEmpty(oldest.Kategori) ? oldest.Kategori
                                   : oldest.Judul;
                    return new CertificateChainGroup
                    {
                        ChainTitle = chainTitle,
                        Certificates = certs,
                        LatestValidUntil = certs.First().ValidUntil
                    };
                })
                .OrderByDescending(g => g.LatestValidUntil ?? DateTime.MaxValue)
                .ToList();

            ViewBag.Mode = mode;
            return PartialView("~/Views/Shared/_CertificateHistoryModalContent.cshtml", groups);
        }

        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> RenewalCertificate(int page = 1)
        {
            var allRows = await BuildRenewalRowsAsync();

            var vm = new CertificationManagementViewModel
            {
                TotalCount = allRows.Count,
                ExpiredCount = allRows.Count(r => r.Status == CertificateStatus.Expired),
                AkanExpiredCount = allRows.Count(r => r.Status == CertificateStatus.AkanExpired),

            };

            ViewBag.AllBagian = await _context.GetAllSectionsAsync();

            ViewBag.AllCategories = await _context.AssessmentCategories
                .Where(c => c.ParentId == null && c.IsActive)
                .OrderBy(c => c.SortOrder)
                .Select(c => c.Name)
                .ToListAsync();

            ViewBag.SelectedView = "RenewalCertificate";

            return View(vm);
        }

        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> FilterRenewalCertificate(
            string? bagian = null,
            string? unit = null,
            string? status = null,
            string? category = null,
            string? subCategory = null,
            string? tipe = null,
            int page = 1)
        {
            var allRows = await BuildRenewalRowsAsync();

            if (!string.IsNullOrEmpty(bagian))
                allRows = allRows.Where(r => r.Bagian == bagian).ToList();
            if (!string.IsNullOrEmpty(unit))
                allRows = allRows.Where(r => r.Unit == unit).ToList();
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<CertificateStatus>(status, out var st))
                allRows = allRows.Where(r => r.Status == st).ToList();
            if (!string.IsNullOrEmpty(category))
                allRows = allRows.Where(r => r.Kategori == category).ToList();
            if (!string.IsNullOrEmpty(subCategory))
                allRows = allRows.Where(r => r.SubKategori == subCategory).ToList();
            if (!string.IsNullOrEmpty(tipe) && Enum.TryParse<RecordType>(tipe, out var rt))
                allRows = allRows.Where(r => r.RecordType == rt).ToList();

            allRows = allRows
                .OrderBy(r => r.Status == CertificateStatus.Expired ? 0 : 1)
                .ThenBy(r => r.ValidUntil ?? DateTime.MaxValue)
                .ToList();

            // Group by judul sertifikat
            var grouped = allRows
                .GroupBy(r => r.Judul, StringComparer.OrdinalIgnoreCase)
                .Select(g => new RenewalGroup
                {
                    GroupKey = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(g.Key))
                                     .Replace("+", "_").Replace("/", "-").Replace("=", ""),
                    Judul = g.Key,
                    Kategori = g.First().Kategori,
                    SubKategori = g.First().SubKategori,
                    TotalCount = g.Count(),
                    ExpiredCount = g.Count(r => r.Status == CertificateStatus.Expired),
                    AkanExpiredCount = g.Count(r => r.Status == CertificateStatus.AkanExpired),
                    MinValidUntil = g.Min(r => r.ValidUntil)
                })
                .OrderBy(g => g.MinValidUntil ?? DateTime.MaxValue)
                .ToList();

            foreach (var group in grouped)
            {
                var groupRows = allRows
                    .Where(r => string.Equals(r.Judul, group.Judul, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(r => r.Status == CertificateStatus.Expired ? 0 : 1)
                    .ThenBy(r => r.ValidUntil ?? DateTime.MaxValue)
                    .ToList();
                var paging = PaginationHelper.Calculate(groupRows.Count, 1, group.PageSize);
                group.Rows = groupRows.Skip(paging.Skip).Take(paging.Take).ToList();
                group.CurrentPage = paging.CurrentPage;
                group.TotalPages = paging.TotalPages;
            }

            var gvm = new RenewalGroupViewModel
            {
                Groups = grouped,
                TotalExpiredCount = allRows.Count(r => r.Status == CertificateStatus.Expired),
                TotalAkanExpiredCount = allRows.Count(r => r.Status == CertificateStatus.AkanExpired)
            };
            gvm.IsFiltered = !string.IsNullOrEmpty(bagian) || !string.IsNullOrEmpty(unit)
                || !string.IsNullOrEmpty(category) || !string.IsNullOrEmpty(subCategory)
                || !string.IsNullOrEmpty(status) || !string.IsNullOrEmpty(tipe);

            return PartialView("~/Views/Shared/_RenewalGroupedPartial.cshtml", gvm);
        }

        [HttpGet]
        [Authorize(Roles = "Admin, HC")]
        public async Task<IActionResult> FilterRenewalCertificateGroup(
            string groupKey,
            string judul,
            int page = 1,
            string? bagian = null, string? unit = null,
            string? status = null, string? category = null, string? subCategory = null,
            string? tipe = null)
        {
            judul = Uri.UnescapeDataString(judul ?? "");
            var allRows = await BuildRenewalRowsAsync();

            if (!string.IsNullOrEmpty(bagian))
                allRows = allRows.Where(r => r.Bagian == bagian).ToList();
            if (!string.IsNullOrEmpty(unit))
                allRows = allRows.Where(r => r.Unit == unit).ToList();
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<CertificateStatus>(status, out var st))
                allRows = allRows.Where(r => r.Status == st).ToList();
            if (!string.IsNullOrEmpty(category))
                allRows = allRows.Where(r => r.Kategori == category).ToList();
            if (!string.IsNullOrEmpty(subCategory))
                allRows = allRows.Where(r => r.SubKategori == subCategory).ToList();
            if (!string.IsNullOrEmpty(tipe) && Enum.TryParse<RecordType>(tipe, out var rt))
                allRows = allRows.Where(r => r.RecordType == rt).ToList();

            var groupRows = allRows
                .Where(r => string.Equals(r.Judul, judul, StringComparison.OrdinalIgnoreCase))
                .OrderBy(r => r.Status == CertificateStatus.Expired ? 0 : 1)
                .ThenBy(r => r.ValidUntil ?? DateTime.MaxValue)
                .ToList();

            var paging = PaginationHelper.Calculate(groupRows.Count, page, 10);
            var group = new RenewalGroup
            {
                GroupKey = groupKey,
                Judul = judul,
                Rows = groupRows.Skip(paging.Skip).Take(paging.Take).ToList(),
                CurrentPage = paging.CurrentPage,
                TotalPages = paging.TotalPages,
                TotalCount = groupRows.Count,
                ExpiredCount = groupRows.Count(r => r.Status == CertificateStatus.Expired),
                AkanExpiredCount = groupRows.Count(r => r.Status == CertificateStatus.AkanExpired)
            };

            return PartialView("~/Views/Shared/_RenewalGroupTablePartial.cshtml", group);
        }
    }
}
