using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HcPortal.Services;
using System.Security.Claims;

namespace HcPortal.Controllers
{
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User ID claim not found");

        [HttpGet]
        public async Task<IActionResult> List()
        {
            var userId = GetUserId();
            var notifications = await _notificationService.GetAsync(userId, 20);

            var result = notifications.Select(n => new
            {
                id = n.Id,
                type = n.Type,
                title = n.Title,
                message = n.Message,
                actionUrl = n.ActionUrl,
                isRead = n.IsRead,
                createdAt = FormatRelativeTime(n.CreatedAt)
            });

            return Json(result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var userId = GetUserId();
            var success = await _notificationService.MarkAsReadAsync(id, userId);
            var unreadCount = await _notificationService.GetUnreadCountAsync(userId);
            return Json(new { success, unreadCount });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = GetUserId();
            await _notificationService.MarkAllAsReadAsync(userId);
            return Json(new { success = true, unreadCount = 0 });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Dismiss(int id)
        {
            var userId = GetUserId();
            var success = await _notificationService.DeleteAsync(id, userId);
            var unreadCount = await _notificationService.GetUnreadCountAsync(userId);
            return Json(new { success, unreadCount });
        }

        [HttpGet]
        public async Task<IActionResult> UnreadCount()
        {
            var userId = GetUserId();
            var count = await _notificationService.GetUnreadCountAsync(userId);
            return Json(new { count });
        }

        private static string FormatRelativeTime(DateTime createdAt)
        {
            var diff = DateTime.UtcNow - createdAt;

            if (diff.TotalMinutes < 1)
                return "Baru saja";
            if (diff.TotalMinutes < 60)
                return $"{(int)diff.TotalMinutes} menit lalu";
            if (diff.TotalHours < 24)
                return $"{(int)diff.TotalHours} jam lalu";
            if (diff.TotalDays < 2)
                return "Kemarin";
            if (diff.TotalDays < 7)
                return $"{(int)diff.TotalDays} hari lalu";

            return createdAt.ToString("dd MMM yyyy");
        }
    }
}
