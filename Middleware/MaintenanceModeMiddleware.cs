using HcPortal.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace HcPortal.Middleware
{
    public class MaintenanceModeMiddleware
    {
        private readonly RequestDelegate _next;
        private const string CacheKey = "MaintenanceMode_State";

        public MaintenanceModeMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IMemoryCache cache, ApplicationDbContext db)
        {
            var path = context.Request.Path;

            // 1. Bypass static files
            if (path.StartsWithSegments("/css") || path.StartsWithSegments("/js") ||
                path.StartsWithSegments("/lib") || path.StartsWithSegments("/images") ||
                path.StartsWithSegments("/favicon.ico"))
            {
                await _next(context);
                return;
            }

            // 2. Bypass maintenance page itself (prevent redirect loop)
            if (path.StartsWithSegments("/Home/Maintenance"))
            {
                await _next(context);
                return;
            }

            // 3. Bypass login and access denied
            if (path.StartsWithSegments("/Account/Login") || path.StartsWithSegments("/Account/AccessDenied"))
            {
                await _next(context);
                return;
            }

            // 4. Bypass SignalR hubs
            if (path.StartsWithSegments("/hubs"))
            {
                await _next(context);
                return;
            }

            // 5. Unauthenticated users bypass — let [Authorize] handle redirect to login
            if (context.User.Identity?.IsAuthenticated != true)
            {
                await _next(context);
                return;
            }

            // 6. Admin + HC bypass
            if (context.User.IsInRole("Admin") || context.User.IsInRole("HC"))
            {
                await _next(context);
                return;
            }

            // 6. Check maintenance state (cached 5 min)
            var maintenance = await GetCachedState(cache, db);
            if (maintenance == null || !maintenance.IsEnabled)
            {
                await _next(context);
                return;
            }

            // 7. Check partial scope
            if (maintenance.Scope != "All")
            {
                var module = GetModuleFromPath(path);
                var modules = maintenance.Scope.Split(',', StringSplitOptions.RemoveEmptyEntries);
                if (!modules.Contains(module, StringComparer.OrdinalIgnoreCase))
                {
                    await _next(context); // Module ini tidak di-maintenance
                    return;
                }
            }

            // 8. AJAX request: return 503
            if (context.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                context.Response.StatusCode = 503;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync("{\"error\":\"Sistem sedang dalam pemeliharaan\"}");
                return;
            }

            // 9. Redirect ke maintenance page
            context.Response.Redirect("/Home/Maintenance");
        }

        private static async Task<HcPortal.Models.MaintenanceMode?> GetCachedState(
            IMemoryCache cache, ApplicationDbContext db)
        {
            if (!cache.TryGetValue(CacheKey, out HcPortal.Models.MaintenanceMode? state))
            {
                state = await db.MaintenanceModes.FirstOrDefaultAsync();
                if (state != null)
                {
                    cache.Set(CacheKey, state, TimeSpan.FromMinutes(5));
                }
            }
            return state;
        }

        private static string GetModuleFromPath(PathString path)
        {
            var segments = path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments == null || segments.Length == 0) return "Home";

            return segments[0] switch
            {
                "CMP" => "CMP",
                "CDP" => "CDP",
                "ProtonData" => "Proton",
                "Admin" => "Admin",
                "Home" => "Home",
                "Account" => "Account",
                "Notification" => "Notification",
                _ => "Home"
            };
        }
    }
}
