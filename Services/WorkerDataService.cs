using HcPortal.Data;
using HcPortal.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HcPortal.Services
{
    public class WorkerDataService : IWorkerDataService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly INotificationService _notificationService;
        private readonly ILogger<WorkerDataService> _logger;

        public WorkerDataService(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            INotificationService notificationService,
            ILogger<WorkerDataService> logger)
        {
            _context = context;
            _userManager = userManager;
            _notificationService = notificationService;
            _logger = logger;
        }

        // CMP version used as base — includes AssessmentSessionId and GenerateCertificate superset fields
        public async Task<List<UnifiedTrainingRecord>> GetUnifiedRecords(string userId)
        {
            // Query 1: Completed assessments only
            var assessments = await _context.AssessmentSessions
                .AsNoTracking()
                .Where(a => a.UserId == userId && a.Status == "Completed")
                .ToListAsync();

            // Query 2: All training records
            var trainings = await _context.TrainingRecords
                .AsNoTracking()
                .Where(t => t.UserId == userId)
                .ToListAsync();

            var unified = new List<UnifiedTrainingRecord>();

            unified.AddRange(assessments.Select(a => new UnifiedTrainingRecord
            {
                Date = a.CompletedAt ?? a.Schedule,
                RecordType = "Assessment Online",
                Title = a.Title,
                Score = a.Score,
                IsPassed = a.IsPassed,
                // Phase 345 CMP06R-04: null = Menunggu Penilaian (label-unify dari Phase 337 "Completed")
                Status = a.IsPassed switch
                {
                    true => "Passed",
                    false => "Failed",
                    null => AssessmentConstants.AssessmentStatus.PendingGrading
                },
                SortPriority = 0,
                AssessmentSessionId = a.Id,
                GenerateCertificate = a.GenerateCertificate,
                Kategori = a.Category
            }));

            unified.AddRange(trainings.Select(t => new UnifiedTrainingRecord
            {
                Date = t.Tanggal,
                RecordType = "Training Manual",
                Title = t.Judul ?? "",
                Penyelenggara = t.Penyelenggara,
                CertificateType = t.CertificateType,
                ValidUntil = t.ValidUntil,
                Status = t.Status,
                SertifikatUrl = t.SertifikatUrl,
                SortPriority = 1,
                TrainingRecordId = t.Id,
                Kategori = t.Kategori,
                SubKategori = t.SubKategori,
                Kota = t.Kota,
                NomorSertifikat = t.NomorSertifikat,
                TanggalMulai = t.TanggalMulai,
                TanggalSelesai = t.TanggalSelesai
            }));

            return unified
                .OrderByDescending(r => r.Date)
                .ThenBy(r => r.SortPriority)
                .ToList();
        }

        // Phase 337 CMP-24: IQueryable composition + Select projection (SQL push-down)
        // Optional filter params untuk Export endpoints — backward compat call tanpa argumen
        public async Task<(List<AllWorkersHistoryRow> assessment, List<AllWorkersHistoryRow> training)>
            GetAllWorkersHistory(
                IEnumerable<string>? workerIds = null,
                DateTime? from = null,
                DateTime? to = null,
                string? category = null,
                string? subCategory = null)
        {
            var workerIdList = workerIds?.ToList();
            bool hasWorkerFilter = workerIdList != null;

            // --- Archived assessment history ---
            var archivedQuery = _context.AssessmentAttemptHistory
                .AsNoTracking()
                .AsQueryable();

            if (hasWorkerFilter)
                archivedQuery = archivedQuery.Where(h => workerIdList!.Contains(h.UserId));
            if (from.HasValue)
                archivedQuery = archivedQuery.Where(h =>
                    (h.CompletedAt ?? h.StartedAt ?? h.ArchivedAt) >= from.Value);
            if (to.HasValue)
                archivedQuery = archivedQuery.Where(h =>
                    (h.CompletedAt ?? h.StartedAt ?? h.ArchivedAt) <= to.Value);
            // Note: AssessmentAttemptHistory tidak punya Category column — skip cat/subcat filter

            var archivedRows = await archivedQuery
                .Select(h => new AllWorkersHistoryRow
                {
                    WorkerId      = h.UserId,
                    WorkerName    = h.User != null ? h.User.FullName : h.UserId,
                    WorkerNIP     = h.User != null ? h.User.NIP : null,
                    RecordType    = "Assessment Online",
                    Title         = h.Title,
                    Date          = h.CompletedAt ?? h.StartedAt ?? h.ArchivedAt,
                    Score         = h.Score,
                    IsPassed      = h.IsPassed,
                    AttemptNumber = h.AttemptNumber
                })
                .ToListAsync();

            // --- Current completed sessions ---
            var currentQuery = _context.AssessmentSessions
                .AsNoTracking()
                .Where(a => a.Status == "Completed");

            if (hasWorkerFilter)
                currentQuery = currentQuery.Where(a => workerIdList!.Contains(a.UserId));
            if (from.HasValue)
                currentQuery = currentQuery.Where(a => (a.CompletedAt ?? a.Schedule) >= from.Value);
            if (to.HasValue)
                currentQuery = currentQuery.Where(a => (a.CompletedAt ?? a.Schedule) <= to.Value);

            var currentRowsRaw = await currentQuery
                .Select(a => new
                {
                    a.Id,   // Phase 338 CIL-03: SessionId untuk drill-down
                    a.UserId,
                    UserFullName = a.User != null ? a.User.FullName : a.UserId,
                    UserNIP = a.User != null ? a.User.NIP : null,
                    a.Title,
                    Date = a.CompletedAt ?? a.Schedule,
                    a.Score,
                    a.IsPassed
                })
                .ToListAsync();

            // archivedCountLookup untuk AttemptNumber (cross-table count, scoped ke workerIds)
            var archivedCountQuery = _context.AssessmentAttemptHistory
                .AsNoTracking()
                .Where(h => h.Title != null);
            if (hasWorkerFilter)
                archivedCountQuery = archivedCountQuery.Where(h => workerIdList!.Contains(h.UserId));
            var archivedCounts = await archivedCountQuery
                .GroupBy(h => new { h.UserId, h.Title })
                .Select(g => new { g.Key.UserId, g.Key.Title, Count = g.Count() })
                .ToListAsync();
            var archivedCountLookup = archivedCounts
                .ToDictionary(x => (x.UserId, x.Title!), x => x.Count);

            var currentRows = currentRowsRaw.Select(a =>
            {
                // Phase 337 CMP-11: title-null tidak collide ke key "" — default ke 1
                int attemptNumber;
                if (string.IsNullOrEmpty(a.Title))
                {
                    attemptNumber = 1;
                }
                else
                {
                    var key = (a.UserId, a.Title);
                    int archived = archivedCountLookup.TryGetValue(key, out var c) ? c : 0;
                    attemptNumber = archived + 1;
                }
                return new AllWorkersHistoryRow
                {
                    WorkerId      = a.UserId,
                    WorkerName    = a.UserFullName,
                    WorkerNIP     = a.UserNIP,
                    RecordType    = "Assessment Online",
                    Title         = a.Title ?? "",
                    Date          = a.Date,
                    Score         = a.Score,
                    IsPassed      = a.IsPassed,
                    AttemptNumber = attemptNumber,
                    SessionId     = a.Id   // Phase 338 CIL-03 D-09: drill-down /CMP/Results/{id}
                };
            }).ToList();

            var assessmentRows = archivedRows.Concat(currentRows)
                .OrderBy(r => r.Title)
                .ThenByDescending(r => r.Date)
                .ToList();

            // --- Training history (CMP-24 SQL push-down) ---
            var trainingsQuery = _context.TrainingRecords
                .AsNoTracking()
                .AsQueryable();

            if (hasWorkerFilter)
                trainingsQuery = trainingsQuery.Where(t => workerIdList!.Contains(t.UserId));
            if (from.HasValue)
                trainingsQuery = trainingsQuery.Where(t => (t.TanggalMulai ?? t.Tanggal) >= from.Value);
            if (to.HasValue)
                trainingsQuery = trainingsQuery.Where(t => (t.TanggalMulai ?? t.Tanggal) <= to.Value);
            if (!string.IsNullOrEmpty(category))
                trainingsQuery = trainingsQuery.Where(t => t.Kategori == category);
            if (!string.IsNullOrEmpty(subCategory))
                trainingsQuery = trainingsQuery.Where(t => t.SubKategori == subCategory);

            var trainingRows = await trainingsQuery
                .Select(t => new AllWorkersHistoryRow
                {
                    WorkerId      = t.UserId,
                    WorkerName    = t.User != null ? t.User.FullName : t.UserId,
                    WorkerNIP     = t.User != null ? t.User.NIP : null,
                    RecordType    = "Manual",
                    Title         = t.Judul ?? "",
                    Date          = t.TanggalMulai ?? t.Tanggal,
                    Penyelenggara = t.Penyelenggara,
                    Kategori      = t.Kategori,
                    SubKategori   = t.SubKategori
                })
                .OrderByDescending(r => r.Date)
                .ToListAsync();

            return (assessmentRows, trainingRows);
        }

        // Admin version used as base — includes IsActive filter that CMP version is missing
        public async Task<List<WorkerTrainingStatus>> GetWorkersInSection(string? section, string? unitFilter = null, string? category = null, string? search = null, string? statusFilter = null, DateTime? dateFrom = null, DateTime? dateTo = null, string? subCategory = null, string? searchScope = null)
        {
            var usersQuery = _context.Users
                .Where(u => u.IsActive)
                .AsQueryable();
            // Phase 337 CMP-25: HAPUS .Include(TrainingRecords) — load separate dengan SQL date filter

            if (!string.IsNullOrEmpty(section))
                usersQuery = usersQuery.Where(u => u.Section == section);

            if (!string.IsNullOrEmpty(unitFilter))
                usersQuery = usersQuery.Where(u => u.Unit == unitFilter);

            // REC-06 D-07: SQL name pre-narrow HANYA untuk scope "Nama".
            // "Training"/"Keduanya" di-handle post-load (union) supaya tidak buang training-only match.
            if (searchScope == "Nama" && !string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                usersQuery = usersQuery.Where(u =>
                    u.FullName.ToLower().Contains(search) ||
                    (u.NIP != null && u.NIP.Contains(search))
                );
            }

            var users = await usersQuery.AsNoTracking().ToListAsync();
            var userIds = users.Select(u => u.Id).ToList();

            bool hasDateFilter = dateFrom.HasValue || dateTo.HasValue;

            // Phase 337 CMP-25: TrainingRecords dengan SQL date filter (composed Where)
            var trainingsQuery = _context.TrainingRecords
                .AsNoTracking()
                .Where(tr => userIds.Contains(tr.UserId));
            if (dateFrom.HasValue)
                trainingsQuery = trainingsQuery.Where(tr => (tr.TanggalMulai ?? tr.Tanggal) >= dateFrom.Value);
            if (dateTo.HasValue)
                trainingsQuery = trainingsQuery.Where(tr => (tr.TanggalMulai ?? tr.Tanggal) <= dateTo.Value);
            var trainingsByUser = (await trainingsQuery.ToListAsync())
                .GroupBy(tr => tr.UserId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Phase 337 CMP-25: AssessmentSessions dengan SQL date filter
            var sessionsQuery = _context.AssessmentSessions
                .AsNoTracking()
                .Where(a => userIds.Contains(a.UserId));
            if (dateFrom.HasValue)
                sessionsQuery = sessionsQuery.Where(a => (a.CompletedAt ?? a.Schedule) >= dateFrom.Value);
            if (dateTo.HasValue)
                sessionsQuery = sessionsQuery.Where(a => (a.CompletedAt ?? a.Schedule) <= dateTo.Value);
            var sessionsByUser = (await sessionsQuery.ToListAsync())
                .GroupBy(a => a.UserId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // passedAssessmentLookup — pakai sessionsByUser bila date filter aktif, else fresh count query
            Dictionary<string, int> passedAssessmentLookup;
            if (hasDateFilter)
            {
                passedAssessmentLookup = sessionsByUser
                    .ToDictionary(kv => kv.Key, kv => kv.Value.Count(a => a.IsPassed == true));
            }
            else
            {
                passedAssessmentLookup = await _context.AssessmentSessions
                    .AsNoTracking()
                    .Where(a => userIds.Contains(a.UserId) && a.IsPassed == true)
                    .GroupBy(a => a.UserId)
                    .Select(g => new { UserId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.UserId, x => x.Count);
            }

            var workerList = new List<WorkerTrainingStatus>();

            foreach (var user in users)
            {
                var trainingRecords = trainingsByUser.TryGetValue(user.Id, out var trs)
                    ? trs : new List<TrainingRecord>();
                var assessmentSessions = sessionsByUser.TryGetValue(user.Id, out var sess)
                    ? sess : new List<AssessmentSession>();

                // Skip worker if no records fall within date range (CMP-25 — preserve existing semantic)
                if (hasDateFilter && trainingRecords.Count == 0 && assessmentSessions.Count == 0)
                    continue;

                int completedAssessments = passedAssessmentLookup.TryGetValue(user.Id, out var aCount) ? aCount : 0;

                var totalTrainings = trainingRecords.Count;
                var completedTrainings = trainingRecords.Count(tr =>
                    tr.Status == "Passed" || tr.Status == "Valid" || tr.Status == "Permanent"
                );
                var pendingTrainings = trainingRecords.Count(tr =>
                    tr.Status == "Pending"
                );
                var expiringTrainings = trainingRecords.Count(tr => tr.IsExpiringSoon);

                var worker = new WorkerTrainingStatus
                {
                    WorkerId = user.Id,
                    WorkerName = user.FullName,
                    NIP = user.NIP,
                    Position = user.Position ?? "Staff",
                    Section = user.Section ?? "",
                    Unit = user.Unit ?? "",
                    TotalTrainings = totalTrainings,
                    CompletedTrainings = completedTrainings,
                    PendingTrainings = pendingTrainings,
                    ExpiringSoonTrainings = expiringTrainings,
                    TrainingRecords = trainingRecords,
                    CompletedAssessments = completedAssessments,
                    AssessmentSessions = assessmentSessions
                };

                if (!string.IsNullOrEmpty(category))
                {
                    bool isCompleted = trainingRecords.Any(r =>
                        !string.IsNullOrEmpty(r.Kategori) &&
                        string.Equals(r.Kategori, category, StringComparison.OrdinalIgnoreCase) &&
                        (r.Status == "Passed" || r.Status == "Valid" || r.Status == "Permanent")
                    );
                    worker.CompletionPercentage = isCompleted ? 100 : 0;
                }
                else
                {
                    worker.CompletionPercentage = totalTrainings > 0
                        ? (int)((double)completedTrainings / totalTrainings * 100)
                        : 0;
                }

                workerList.Add(worker);
            }

            // Phase 337 CMP-03: Category narrow workerList (bukan hanya set CompletionPercentage)
            if (!string.IsNullOrEmpty(category))
            {
                workerList = workerList.Where(w =>
                    w.TrainingRecords.Any(t => !string.IsNullOrEmpty(t.Kategori) &&
                                               string.Equals(t.Kategori, category, StringComparison.OrdinalIgnoreCase))
                    || w.AssessmentSessions.Any(a => !string.IsNullOrEmpty(a.Category) &&
                                                      string.Equals(a.Category, category, StringComparison.OrdinalIgnoreCase))
                ).ToList();
            }

            // Phase 337 CMP-02: SubCategory narrow workerList
            if (!string.IsNullOrEmpty(subCategory))
            {
                workerList = workerList.Where(w =>
                    w.TrainingRecords.Any(t => !string.IsNullOrEmpty(t.SubKategori) &&
                                               string.Equals(t.SubKategori, subCategory, StringComparison.OrdinalIgnoreCase))
                ).ToList();
            }

            // Phase 337 CMP-01: statusFilter apply mandiri (guard `&& !string.IsNullOrEmpty(category)` dihapus)
            if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "ALL")
            {
                if (statusFilter == "Sudah")
                    workerList = workerList.Where(w => w.CompletionPercentage == 100).ToList();
                else if (statusFilter == "Belum")
                    workerList = workerList.Where(w => w.CompletionPercentage != 100).ToList();
            }

            // REC-06 D-07: Training/Keduanya search = post-load in-memory filter (menyaring worker mana yang muncul; badge count per-worker tetap utuh).
            if (!string.IsNullOrEmpty(search) && (searchScope == "Training" || searchScope == "Keduanya"))
            {
                var searchLower = search.ToLower();
                workerList = workerList.Where(w =>
                {
                    bool trainingMatch = w.TrainingRecords != null &&
                        w.TrainingRecords.Any(t => !string.IsNullOrEmpty(t.Judul) &&
                                                   t.Judul.ToLower().Contains(searchLower));
                    if (searchScope == "Training") return trainingMatch;
                    // Keduanya: union Nama/NIP OR Training
                    bool nameMatch =
                        (!string.IsNullOrEmpty(w.WorkerName) && w.WorkerName.ToLower().Contains(searchLower)) ||
                        (!string.IsNullOrEmpty(w.NIP) && w.NIP.ToLower().Contains(searchLower));
                    return nameMatch || trainingMatch;
                }).ToList();
            }

            return workerList;
        }

        // Admin version — allows Cancelled status (correct per user decision)
        public async Task NotifyIfGroupCompleted(AssessmentSession completedSession)
        {
            var allSiblings = await _context.AssessmentSessions
                .AsNoTracking()
                .Where(s => s.Title == completedSession.Title &&
                            s.Category == completedSession.Category &&
                            s.Schedule.Date == completedSession.Schedule.Date)
                .ToListAsync();

            // Group is "done" when every session is either Completed or Cancelled (no Open/InProgress left)
            if (!allSiblings.All(s => s.Status == "Completed" || s.Status == "Cancelled")) return;

            var hcUsers = await _userManager.GetUsersInRoleAsync("HC");
            var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
            var recipientIds = hcUsers.Concat(adminUsers)
                .Select(u => u.Id).Distinct().ToList();

            foreach (var recipientId in recipientIds)
            {
                // Phase 310 D-05 — dedup via UserNotifications.AnyAsync sebelum SendAsync
                // Schema: UserNotifications TIDAK punya SourceTitle/SourceDate, jadi pakai
                // Type + Title exact + Message.Contains(sessionTitle) + CreatedAt time-window guard
                // Phase 310 WR-03 — pakai UTC-bounded window (-2 hari) untuk hindari timezone mismatch
                // dan tidak ada upper bound False-Positive (Schedule.Date bisa local-time, CreatedAt UTC)
                var windowStart = DateTime.UtcNow.AddDays(-2);
                bool alreadySent = await _context.UserNotifications.AnyAsync(n =>
                    n.UserId == recipientId
                    && n.Type == "ASMT_ALL_COMPLETED"
                    && n.Title == "Assessment Selesai"
                    && n.Message.Contains(completedSession.Title)
                    && n.CreatedAt >= windowStart);

                if (alreadySent)
                {
                    _logger.LogInformation(
                        "NotifyIfGroupCompleted: skip recipient {RecipientId} — sudah ada notif untuk session \"{Title}\"",
                        recipientId, completedSession.Title);
                    continue;
                }

                try
                {
                    await _notificationService.SendAsync(
                        recipientId,
                        "ASMT_ALL_COMPLETED",
                        "Assessment Selesai",
                        $"Semua peserta assessment \"{completedSession.Title}\" telah menyelesaikan ujian",
                        "/CMP/Assessment"
                    );
                }
                catch (Exception ex) { _logger.LogWarning(ex, "Notification send failed"); }
            }
        }
    }
}
