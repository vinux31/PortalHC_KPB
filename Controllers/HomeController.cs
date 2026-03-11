using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using HcPortal.Models;
using HcPortal.Data;

namespace HcPortal.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;

    public HomeController(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var upcomingEvents = await GetUpcomingEvents(user.Id);
        var progress = await GetProgress(user.Id);

        var viewModel = new DashboardHomeViewModel
        {
            CurrentUser = user,
            Greeting = GetTimeBasedGreeting(),
            UpcomingEvents = upcomingEvents,
            Progress = progress
        };

        return View(viewModel);
    }

    private async Task<ProgressViewModel> GetProgress(string userId)
    {
        var progress = new ProgressViewModel();

        var trackAssignment = await _context.ProtonTrackAssignments
            .Include(t => t.ProtonTrack)
            .Where(t => t.CoacheeId == userId && t.IsActive)
            .FirstOrDefaultAsync();

        if (trackAssignment != null)
        {
            progress.CdpTrackName = trackAssignment.ProtonTrack?.DisplayName ?? "";

            var allDeliverableIds = await _context.ProtonKompetensiList
                .Where(k => k.ProtonTrackId == trackAssignment.ProtonTrackId)
                .SelectMany(k => k.SubKompetensiList)
                .SelectMany(s => s.Deliverables)
                .Select(d => d.Id)
                .ToListAsync();

            progress.CdpTotal = allDeliverableIds.Count;

            if (progress.CdpTotal > 0)
            {
                var completedCount = await _context.ProtonDeliverableProgresses
                    .Where(p => p.CoacheeId == userId && 
                           p.ProtonTrackAssignmentId == trackAssignment.Id &&
                           p.Status == "Approved")
                    .CountAsync();

                progress.CdpCompleted = completedCount;
                progress.CdpProgress = (int)Math.Round((double)completedCount / progress.CdpTotal * 100);
            }
        }

        var totalAssessments = await _context.AssessmentSessions
            .Where(a => a.UserId == userId)
            .CountAsync();

        var completedAssessments = await _context.AssessmentSessions
            .Where(a => a.UserId == userId && a.Status == "Completed")
            .CountAsync();

        progress.AssessmentTotal = totalAssessments;
        progress.AssessmentCompleted = completedAssessments;
        progress.AssessmentProgress = totalAssessments > 0 
            ? (int)Math.Round((double)completedAssessments / totalAssessments * 100) 
            : 0;

        var coachingSessions = await _context.CoachingSessions
            .Where(c => c.CoacheeId == userId)
            .ToListAsync();

        progress.CoachingTotal = coachingSessions.Count;
        progress.CoachingCompleted = coachingSessions.Count(c => c.Status == "Submitted");
        progress.CoachingProgress = progress.CoachingTotal > 0
            ? (int)Math.Round((double)progress.CoachingCompleted / progress.CoachingTotal * 100)
            : 0;

        return progress;
    }

    private async Task<List<UpcomingEventViewModel>> GetUpcomingEvents(string userId)
    {
        var events = new List<UpcomingEventViewModel>();

        var today = DateTime.Today;
        var tomorrow = DateTime.Today.AddDays(2).AddTicks(-1); // end of tomorrow

        var upcomingCoachings = await _context.CoachingSessions
            .Where(c => c.CoacheeId == userId && c.Date >= today && c.Date <= tomorrow)
            .OrderBy(c => c.Date)
            .ToListAsync();

        foreach (var coaching in upcomingCoachings)
        {
            events.Add(new UpcomingEventViewModel
            {
                Type = "Coaching",
                Title = $"Coaching: {coaching.Kompetensi}",
                Description = coaching.SubKompetensi,
                Date = coaching.Date,
                Icon = "bi-people",
                Color = "warning",
                Url = "/CDP/CoachingProton"
            });
        }

        var pendingAssessments = await _context.AssessmentSessions
            .Where(a => a.UserId == userId &&
                   (a.Status == "Open" || a.Status == "Upcoming") &&
                   (a.ExamWindowCloseDate == null || a.ExamWindowCloseDate > DateTime.Now) &&
                   a.Schedule >= today && a.Schedule <= tomorrow)
            .OrderBy(a => a.Schedule)
            .ToListAsync();

        foreach (var assessment in pendingAssessments)
        {
            var isStarted = assessment.StartedAt.HasValue;
            events.Add(new UpcomingEventViewModel
            {
                Type = "Assessment",
                Title = assessment.Title,
                Description = isStarted ? "Sedang berlangsung" : assessment.Category,
                Date = assessment.Schedule,
                Icon = "bi-clipboard-check",
                Color = isStarted ? "danger" : "success",
                Url = "/CMP/Assessment"
            });
        }

        return events.OrderBy(e => e.Date).Take(5).ToList();
    }

    public async Task<IActionResult> Guide()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();
        var userRoles = await _userManager.GetRolesAsync(user);
        ViewBag.UserRole = userRoles.FirstOrDefault() ?? "User";
        return View();
    }

    public async Task<IActionResult> GuideDetail(string module)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();
        var userRoles = await _userManager.GetRolesAsync(user);
        var userRole = userRoles.FirstOrDefault() ?? "User";

        // Validate module & role access
        var adminModules = new[] { "data", "admin" };
        if (adminModules.Contains(module) && userRole != "Admin" && userRole != "HC")
            return RedirectToAction("Guide");

        var validModules = new[] { "cmp", "cdp", "account", "data", "admin" };
        if (!validModules.Contains(module))
            return RedirectToAction("Guide");

        ViewBag.UserRole = userRole;
        ViewBag.Module = module;
        return View();
    }

    private string GetTimeBasedGreeting()
    {
        var hour = DateTime.Now.Hour;
        return hour < 12 ? "Selamat Pagi"
             : hour < 15 ? "Selamat Siang"
             : hour < 18 ? "Selamat Sore"
             : "Selamat Malam";
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
