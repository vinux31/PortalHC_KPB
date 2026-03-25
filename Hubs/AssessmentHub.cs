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
