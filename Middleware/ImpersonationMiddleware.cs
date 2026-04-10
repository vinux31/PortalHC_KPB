using HcPortal.Services;
using HcPortal.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace HcPortal.Middleware
{
    public class ImpersonationMiddleware
    {
        private readonly RequestDelegate _next;

        private static readonly HashSet<string> WhitelistedPaths = new(StringComparer.OrdinalIgnoreCase)
        {
            "/Account/Login",
            "/Account/Logout",
            "/Admin/StopImpersonation",
            "/Admin/SearchUsersApi"
        };

        public ImpersonationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path;

            // Bypass static files
            if (path.StartsWithSegments("/css") || path.StartsWithSegments("/js") ||
                path.StartsWithSegments("/lib") || path.StartsWithSegments("/images") ||
                path.StartsWithSegments("/favicon.ico"))
            {
                await _next(context);
                return;
            }

            // Bypass unauthenticated users
            if (context.User.Identity?.IsAuthenticated != true)
            {
                await _next(context);
                return;
            }

            // Resolve service
            var impersonationService = context.RequestServices.GetRequiredService<ImpersonationService>();

            // Bypass if not impersonating
            if (!impersonationService.IsImpersonating())
            {
                await _next(context);
                return;
            }

            // Check auto-expire (30 minutes)
            if (impersonationService.IsExpired())
            {
                impersonationService.Stop();

                var tempDataFactory = context.RequestServices.GetRequiredService<ITempDataDictionaryFactory>();
                var tempData = tempDataFactory.GetTempData(context);
                tempData["ErrorMessage"] = "Sesi impersonation telah berakhir (batas 30 menit).";

                context.Response.Redirect("/Admin/Index");
                return;
            }

            var method = context.Request.Method;

            // Allow GET and HEAD requests
            if (method == "GET" || method == "HEAD")
            {
                // Set context items and continue
                SetContextItems(context, impersonationService);
                await _next(context);
                return;
            }

            // POST/PUT/DELETE: check whitelist
            var pathValue = path.Value ?? "";
            bool isWhitelisted = WhitelistedPaths.Contains(pathValue) ||
                                 path.StartsWithSegments("/hubs");

            if (!isWhitelisted)
            {
                // Block write operation
                if (context.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    context.Response.StatusCode = 403;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(
                        "{\"error\":\"Aksi ini diblokir \\u2014 Mode Read-Only aktif\",\"readOnly\":true}");
                    return;
                }

                // Non-AJAX: TempData + redirect
                var tempDataFactory = context.RequestServices.GetRequiredService<ITempDataDictionaryFactory>();
                var tempData = tempDataFactory.GetTempData(context);
                tempData["ErrorMessage"] = "Aksi tidak diizinkan dalam mode impersonation. Semua perubahan data diblokir.";

                var referrer = context.Request.Headers["Referer"].FirstOrDefault();
                context.Response.Redirect(!string.IsNullOrEmpty(referrer) ? referrer : "/Home/Index");
                return;
            }

            // Whitelisted write — allow
            SetContextItems(context, impersonationService);
            await _next(context);
        }

        private static void SetContextItems(HttpContext context, ImpersonationService service)
        {
            context.Items["IsImpersonating"] = true;
            context.Items["ImpersonateMode"] = service.GetMode();
            context.Items["ImpersonateTargetName"] = service.GetDisplayName();

            var mode = service.GetMode();
            if (mode == "role")
            {
                var role = service.GetTargetRole();
                context.Items["ImpersonateTargetRole"] = role;
                if (role != null)
                {
                    context.Items["ImpersonateTargetRoleLevel"] = UserRoles.GetRoleLevel(role);
                    context.Items["ImpersonateTargetSelectedView"] = UserRoles.GetDefaultView(role);
                }
            }
            else if (mode == "user")
            {
                // Resolve target user's role info
                var targetUserId = service.GetTargetUserId();
                if (targetUserId != null)
                {
                    var userManager = context.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
                    var targetUser = userManager.FindByIdAsync(targetUserId).GetAwaiter().GetResult();
                    if (targetUser != null)
                    {
                        var roles = userManager.GetRolesAsync(targetUser).GetAwaiter().GetResult();
                        var primaryRole = roles.FirstOrDefault() ?? "Coachee";
                        context.Items["ImpersonateTargetRole"] = primaryRole;
                        context.Items["ImpersonateTargetRoleLevel"] = UserRoles.GetRoleLevel(primaryRole);
                        context.Items["ImpersonateTargetSelectedView"] = targetUser.SelectedView;
                    }
                }
            }
        }
    }
}
