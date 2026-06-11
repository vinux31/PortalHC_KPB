using HcPortal.Models;
using HcPortal.Services;

namespace HcPortal.Tests;

/// <summary>
/// Phase 363-03 — di-lift dari nested private ProtonBypassServiceTests supaya bisa dipakai
/// lintas test file (ProtonCompletionMissTests, NewSvc helpers). Merekam (UserId, Type) ke Sent.
/// </summary>
internal sealed class FakeNotificationService : INotificationService
{
    public List<(string UserId, string Type)> Sent { get; } = new();
    public Task<bool> SendAsync(string userId, string type, string title, string message, string? actionUrl = null)
    { Sent.Add((userId, type)); return Task.FromResult(true); }
    public Task<List<UserNotification>> GetAsync(string userId, int count = 50)
        => Task.FromResult(new List<UserNotification>());
    public Task<bool> MarkAsReadAsync(int notificationId, string userId) => Task.FromResult(true);
    public Task<int> MarkAllAsReadAsync(string userId) => Task.FromResult(0);
    public Task<int> GetUnreadCountAsync(string userId) => Task.FromResult(0);
    public Task<bool> SendByTemplateAsync(string userId, string type, Dictionary<string, object>? context = null)
    { Sent.Add((userId, type)); return Task.FromResult(true); }
    public Task<bool> DeleteAsync(int notificationId, string userId) => Task.FromResult(true);
}
