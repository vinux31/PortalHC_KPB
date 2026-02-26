using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using HcPortal.Models;
using HcPortal.Data;
using HcPortal.Services;

namespace HcPortal.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AuditLogService _auditLog;

        public AdminController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            AuditLogService auditLog)
        {
            _context = context;
            _userManager = userManager;
            _auditLog = auditLog;
        }

        // GET /Admin/Index
        public IActionResult Index()
        {
            return View();
        }

        // GET /Admin/KkjMatrix
        public async Task<IActionResult> KkjMatrix()
        {
            ViewData["Title"] = "Kelola KKJ Matrix";
            var items = await _context.KkjMatrices.OrderBy(k => k.No).ToListAsync();
            return View(items);
        }
    }
}
