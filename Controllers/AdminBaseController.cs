using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using HcPortal.Models;
using HcPortal.Data;
using HcPortal.Services;

namespace HcPortal.Controllers
{
    [Authorize]
    [Route("Admin")]
    [Route("Admin/[action]")]
    public abstract class AdminBaseController : Controller
    {
        protected readonly ApplicationDbContext _context;
        protected readonly UserManager<ApplicationUser> _userManager;
        protected readonly AuditLogService _auditLog;
        protected readonly IWebHostEnvironment _env;

        protected AdminBaseController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            AuditLogService auditLog,
            IWebHostEnvironment env)
        {
            _context = context;
            _userManager = userManager;
            _auditLog = auditLog;
            _env = env;
        }

        protected static string MapKategori(string? raw, Dictionary<string, string>? rawToDisplayMap)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "-";
            var trimmed = raw.Trim();
            if (rawToDisplayMap != null && rawToDisplayMap.TryGetValue(trimmed.ToUpperInvariant(), out var displayName))
                return displayName;
            return trimmed;
        }
    }
}
