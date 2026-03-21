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
                .Where(a => a.UserId == userId && a.Status == "Completed")
                .ToListAsync();

            // Query 2: All training records
            var trainings = await _context.TrainingRecords
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
                Status = a.IsPassed == true ? "Passed" : "Failed",
                SortPriority = 0,
                AssessmentSessionId = a.Id,
                GenerateCertificate = a.GenerateCertificate
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

        // CMP version used as base — includes WorkerId superset fields
        public async Task<(List<AllWorkersHistoryRow> assessment, List<AllWorkersHistoryRow> training)> GetAllWorkersHistory()
        {
            // --- Assessment history ---

            // Batch count archived attempts per user+title to avoid N+1
            var archivedCounts = await _context.AssessmentAttemptHistory
                .GroupBy(h => new { h.UserId, h.Title })
                .Select(g => new { g.Key.UserId, g.Key.Title, Count = g.Count() })
                .ToListAsync();

            var archivedCountLookup = archivedCounts
                .ToDictionary(x => (x.UserId, x.Title), x => x.Count);

            // Query 1: Archived attempts (AttemptNumber already stored)
            var archivedAttempts = await _context.AssessmentAttemptHistory
                .Include(h => h.User)
                .ToListAsync();

            var assessmentRows = new List<AllWorkersHistoryRow>();

            assessmentRows.AddRange(archivedAttempts.Select(h => new AllWorkersHistoryRow
            {
                WorkerId      = h.UserId,
                WorkerName    = h.User?.FullName ?? h.UserId,
                WorkerNIP     = h.User?.NIP,
                RecordType    = "Assessment Online",
                Title         = h.Title,
                Date          = h.CompletedAt ?? h.StartedAt ?? h.ArchivedAt,
                Score         = h.Score,
                IsPassed      = h.IsPassed,
                AttemptNumber = h.AttemptNumber
            }));

            // Query 2: Current completed sessions (Attempt # = archived count for that user+title + 1)
            var currentCompleted = await _context.AssessmentSessions
                .Include(a => a.User)
                .Where(a => a.Status == "Completed")
                .ToListAsync();

            assessmentRows.AddRange(currentCompleted.Select(a =>
            {
                var key = (a.UserId, a.Title ?? "");
                int archived = archivedCountLookup.TryGetValue(key, out var c) ? c : 0;
                return new AllWorkersHistoryRow
                {
                    WorkerId      = a.UserId,
                    WorkerName    = a.User?.FullName ?? a.UserId,
                    WorkerNIP     = a.User?.NIP,
                    RecordType    = "Assessment Online",
                    Title         = a.Title ?? "",
                    Date          = a.CompletedAt ?? a.Schedule,
                    Score         = a.Score,
                    IsPassed      = a.IsPassed,
                    AttemptNumber = archived + 1
                };
            }));

            // Sort: grouped by title then date descending
            assessmentRows = assessmentRows
                .OrderBy(r => r.Title)
                .ThenByDescending(r => r.Date)
                .ToList();

            // --- Training history ---
            var trainings = await _context.TrainingRecords
                .Include(t => t.User)
                .ToListAsync();

            var trainingRows = trainings.Select(t => new AllWorkersHistoryRow
            {
                WorkerId      = t.UserId,
                WorkerName    = t.User?.FullName ?? t.UserId,
                WorkerNIP     = t.User?.NIP,
                RecordType    = "Manual",
                Title         = t.Judul ?? "",
                // PITFALL: TanggalMulai is nullable — must coalesce to Tanggal
                Date          = t.TanggalMulai ?? t.Tanggal,
                Penyelenggara = t.Penyelenggara
            })
            .OrderByDescending(r => r.Date)
            .ToList();

            return (assessmentRows, trainingRows);
        }

        // Admin version used as base — includes IsActive filter that CMP version is missing
        public async Task<List<WorkerTrainingStatus>> GetWorkersInSection(string? section, string? unitFilter = null, string? category = null, string? search = null, string? statusFilter = null)
        {
            var usersQuery = _context.Users
                .Where(u => u.IsActive)
                .Include(u => u.TrainingRecords)
                .AsQueryable();

            if (!string.IsNullOrEmpty(section))
                usersQuery = usersQuery.Where(u => u.Section == section);

            if (!string.IsNullOrEmpty(unitFilter))
                usersQuery = usersQuery.Where(u => u.Unit == unitFilter);

            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                usersQuery = usersQuery.Where(u =>
                    u.FullName.ToLower().Contains(search) ||
                    (u.NIP != null && u.NIP.Contains(search))
                );
            }

            var users = await usersQuery.ToListAsync();

            var userIds = users.Select(u => u.Id).ToList();
            var passedAssessmentsByUser = await _context.AssessmentSessions
                .Where(a => userIds.Contains(a.UserId) && a.IsPassed == true)
                .GroupBy(a => a.UserId)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .ToListAsync();
            var passedAssessmentLookup = passedAssessmentsByUser
                .ToDictionary(x => x.UserId, x => x.Count);

            var workerList = new List<WorkerTrainingStatus>();

            foreach (var user in users)
            {
                var trainingRecords = user.TrainingRecords.ToList();
                int completedAssessments = passedAssessmentLookup.TryGetValue(user.Id, out var aCount) ? aCount : 0;

                var totalTrainings = trainingRecords.Count;
                var completedTrainings = trainingRecords.Count(tr =>
                    tr.Status == "Passed" || tr.Status == "Valid" || tr.Status == "Permanent"
                );
                var pendingTrainings = trainingRecords.Count(tr =>
                    tr.Status == "Wait Certificate" || tr.Status == "Pending"
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
                    CompletedAssessments = completedAssessments
                };

                if (!string.IsNullOrEmpty(category))
                {
                    bool isCompleted = trainingRecords.Any(r =>
                        !string.IsNullOrEmpty(r.Kategori) &&
                        r.Kategori.Contains(category, StringComparison.OrdinalIgnoreCase) &&
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

            if (!string.IsNullOrEmpty(statusFilter) && !string.IsNullOrEmpty(category))
            {
                if (statusFilter == "Sudah")
                    workerList = workerList.Where(w => w.CompletionPercentage == 100).ToList();
                else if (statusFilter == "Belum")
                    workerList = workerList.Where(w => w.CompletionPercentage != 100).ToList();
            }

            return workerList;
        }

        // Admin version — allows Cancelled status (correct per user decision)
        public async Task NotifyIfGroupCompleted(AssessmentSession completedSession)
        {
            var allSiblings = await _context.AssessmentSessions
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
