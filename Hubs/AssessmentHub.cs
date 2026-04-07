using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using HcPortal.Data;
using HcPortal.Models;
using Microsoft.EntityFrameworkCore;

namespace HcPortal.Hubs
{
    [Authorize]
    public class AssessmentHub : Hub
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AssessmentHub> _logger;

        public AssessmentHub(IServiceScopeFactory scopeFactory, ILogger<AssessmentHub> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task JoinBatch(string batchKey)
        {
            var userId = Context.UserIdentifier;
            if (string.IsNullOrEmpty(userId)) return;

            // Verify user has an active (InProgress) session before joining batch group
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var hasSession = await db.AssessmentSessions
                .AnyAsync(s => s.UserId == userId && s.Status == "InProgress");
            if (!hasSession) return;

            await Groups.AddToGroupAsync(Context.ConnectionId, $"batch-{batchKey}");
        }

        public async Task LeaveBatch(string batchKey)
        {
            // LeaveBatch is safe — removing from a group you're not in is a no-op
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"batch-{batchKey}");
        }

        public async Task JoinMonitor(string batchKey)
        {
            var userId = Context.UserIdentifier;
            if (string.IsNullOrEmpty(userId)) return;

            // Verify user is Admin or HC (monitoring role)
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = await db.Users.FindAsync(userId);
            if (user == null) return;

            var userManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<HcPortal.Models.ApplicationUser>>();
            var roles = await userManager.GetRolesAsync(user);
            if (!roles.Contains("Admin") && !roles.Contains("HC")) return;

            await Groups.AddToGroupAsync(Context.ConnectionId, $"monitor-{batchKey}");
        }

        public async Task LeaveMonitor(string batchKey)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"monitor-{batchKey}");
        }

        /// <summary>
        /// Called by exam client JS when worker navigates to a page.
        /// Fire-and-forget DB write via a new scope to avoid DbContext threading issues.
        /// </summary>
        public Task LogPageNav(int sessionId, int pageNumber)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    db.ExamActivityLogs.Add(new ExamActivityLog
                    {
                        SessionId = sessionId,
                        EventType = "page_nav",
                        Detail = $"Halaman {pageNumber + 1}",
                        Timestamp = DateTime.UtcNow
                    });
                    await db.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to log page nav for session={SessionId}", sessionId);
                }
            });
            return Task.CompletedTask;
        }

        public override Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;
            if (!string.IsNullOrEmpty(userId))
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                        var sessionId = await db.AssessmentSessions
                            .Where(s => s.UserId == userId && s.Status == "InProgress")
                            .Select(s => s.Id)
                            .FirstOrDefaultAsync();
                        if (sessionId != 0)
                        {
                            db.ExamActivityLogs.Add(new ExamActivityLog
                            {
                                SessionId = sessionId,
                                EventType = "reconnected",
                                Timestamp = DateTime.UtcNow
                            });
                            await db.SaveChangesAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to log reconnection for user={UserId}", userId);
                    }
                });
            }
            return base.OnConnectedAsync();
        }

        /// <summary>
        /// Menyimpan jawaban teks untuk soal Essay.
        /// T-298-07: validasi sessionId milik user yang terautentikasi.
        /// T-298-09: truncate TextAnswer ke MaxCharacters server-side.
        /// </summary>
        public async Task SaveTextAnswer(int sessionId, int questionId, string textAnswer)
        {
            var userId = Context.UserIdentifier;
            if (string.IsNullOrEmpty(userId)) return;

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // T-298-07: Validasi session milik user ini dan masih InProgress
            var session = await db.AssessmentSessions
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId && s.Status == "InProgress");
            if (session == null)
            {
                _logger.LogWarning("SaveTextAnswer: unauthorized or invalid session {SessionId} for user {UserId}", sessionId, userId);
                return;
            }

            // T-298-09: Ambil MaxCharacters dari soal untuk truncate server-side
            var question = await db.PackageQuestions
                .Where(q => q.Id == questionId)
                .Select(q => new { q.MaxCharacters })
                .FirstOrDefaultAsync();

            int maxChars = question?.MaxCharacters > 0 ? question.MaxCharacters : 2000;
            if (textAnswer != null && textAnswer.Length > maxChars)
                textAnswer = textAnswer.Substring(0, maxChars);

            // Upsert PackageUserResponse: cari existing by sessionId+questionId
            var existing = await db.PackageUserResponses
                .FirstOrDefaultAsync(r => r.AssessmentSessionId == sessionId && r.PackageQuestionId == questionId);

            if (existing != null)
            {
                existing.TextAnswer = textAnswer;
                existing.PackageOptionId = null;
            }
            else
            {
                db.PackageUserResponses.Add(new PackageUserResponse
                {
                    AssessmentSessionId = sessionId,
                    PackageQuestionId = questionId,
                    PackageOptionId = null,
                    TextAnswer = textAnswer
                });
            }

            await db.SaveChangesAsync();
        }

        /// <summary>
        /// Menyimpan jawaban multi-pilihan untuk soal MultipleAnswer.
        /// T-298-08: validasi optionIds milik soal tersebut, validasi session timer belum expired.
        /// </summary>
        public async Task SaveMultipleAnswer(int sessionId, int questionId, string optionIds)
        {
            var userId = Context.UserIdentifier;
            if (string.IsNullOrEmpty(userId)) return;

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // T-298-07 + T-298-08: Validasi session milik user ini, masih InProgress
            var session = await db.AssessmentSessions
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId && s.Status == "InProgress");
            if (session == null)
            {
                _logger.LogWarning("SaveMultipleAnswer: unauthorized or invalid session {SessionId} for user {UserId}", sessionId, userId);
                return;
            }

            // T-298-08: Validasi timer belum expired (server-side check, memperhitungkan ExtraTimeMinutes)
            if (session.StartedAt.HasValue && session.DurationMinutes > 0)
            {
                var elapsed = (DateTime.UtcNow - session.StartedAt.Value).TotalSeconds;
                var allowed = (session.DurationMinutes + (session.ExtraTimeMinutes ?? 0)) * 60;
                if (elapsed > allowed)
                {
                    _logger.LogWarning("SaveMultipleAnswer: timer expired for session {SessionId}", sessionId);
                    return;
                }
            }

            // Parse optionIds dari comma-separated string
            var selectedOptionIds = new List<int>();
            if (!string.IsNullOrWhiteSpace(optionIds))
            {
                foreach (var part in optionIds.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (int.TryParse(part.Trim(), out var oid))
                        selectedOptionIds.Add(oid);
                }
            }

            // T-298-08: Validasi optionIds benar-benar milik questionId ini
            var validOptionIds = await db.PackageOptions
                .Where(o => o.PackageQuestionId == questionId && selectedOptionIds.Contains(o.Id))
                .Select(o => o.Id)
                .ToListAsync();

            // Hapus semua respons existing untuk soal ini
            var existingResponses = db.PackageUserResponses
                .Where(r => r.AssessmentSessionId == sessionId && r.PackageQuestionId == questionId);
            db.PackageUserResponses.RemoveRange(existingResponses);

            // Insert baru per optionId yang valid
            foreach (var oid in validOptionIds)
            {
                db.PackageUserResponses.Add(new PackageUserResponse
                {
                    AssessmentSessionId = sessionId,
                    PackageQuestionId = questionId,
                    PackageOptionId = oid,
                    TextAnswer = null
                });
            }

            await db.SaveChangesAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.UserIdentifier;
            if (!string.IsNullOrEmpty(userId))
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                        var sessionId = await db.AssessmentSessions
                            .Where(s => s.UserId == userId && s.Status == "InProgress")
                            .Select(s => s.Id)
                            .FirstOrDefaultAsync();
                        if (sessionId != 0)
                        {
                            db.ExamActivityLogs.Add(new ExamActivityLog
                            {
                                SessionId = sessionId,
                                EventType = "disconnected",
                                Timestamp = DateTime.UtcNow
                            });
                            await db.SaveChangesAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to log disconnection for user={UserId}", userId);
                    }
                });
            }
            return base.OnDisconnectedAsync(exception);
        }
    }
}
