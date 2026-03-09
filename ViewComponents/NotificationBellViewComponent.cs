using Microsoft.AspNetCore.Mvc;
using HcPortal.Services;
using System.Security.Claims;

namespace HcPortal.ViewComponents
{
    public class NotificationBellViewComponent : ViewComponent
    {
        private readonly INotificationService _notificationService;

        public NotificationBellViewComponent(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var userId = UserClaimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
            var unreadCount = 0;

            if (!string.IsNullOrEmpty(userId))
            {
                unreadCount = await _notificationService.GetUnreadCountAsync(userId);
            }

            return View(unreadCount);
        }
    }
}
