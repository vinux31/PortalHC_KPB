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

        public AssessmentHub(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public async Task JoinBatch(string batchKey)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"batch-{batchKey}");
        }

        public async Task LeaveBatch(string batchKey)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"batch-{batchKey}");
        }

        public async Task JoinMonitor(string batchKey)
        {
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
                        Detail = $"Halaman {pageNumber}",
                        Timestamp = DateTime.UtcNow
                    });
                    await db.SaveChangesAsync();
                }
                catch
                {
                    // Logging must never break exam flow — swallow all errors
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
                    catch
                    {
                        // Swallow — never break SignalR connection handling
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
                    catch
                    {
                        // Swallow — never break SignalR disconnection handling
                    }
                });
            }
            return base.OnDisconnectedAsync(exception);
        }
    }
}
