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

        // Determine target user(s) based on Admin view selection
        var userRoles = await _userManager.GetRolesAsync(user);
        var userRole = userRoles.FirstOrDefault();
        var targetUserIds = new List<string> { user.Id }; // Default: current user only

        // ========== VIEW-BASED FILTERING FOR ADMIN ==========
        if (userRole == UserRoles.Admin)
        {
            if (user.SelectedView == "Coachee" || user.SelectedView == "Coach")
            {
                // Personal view: use current user only (already set as default)
            }
            else if (user.SelectedView == "Atasan" && !string.IsNullOrEmpty(user.Section))
            {
                // Section view: get all users in section
                targetUserIds = await _context.Users
                    .Where(u => u.Section == user.Section)
                    .Select(u => u.Id)
                    .ToListAsync();
            }
            else if (user.SelectedView == "HC")
            {
                // HC view: get all users
                targetUserIds = await _context.Users
                    .Select(u => u.Id)
                    .ToListAsync();
            }
        }

        var viewModel = new DashboardHomeViewModel
        {
            CurrentUser = user,
            Greeting = GetTimeBasedGreeting(),

            // IDP Stats (filtered by target users)
            IdpTotalCount = await _context.IdpItems
                .Where(i => targetUserIds.Contains(i.UserId))
                .CountAsync(),
            IdpCompletedCount = await _context.IdpItems
                .Where(i => targetUserIds.Contains(i.UserId) &&
                       (i.Status == "Completed" || i.Status == "Approved"))
                .CountAsync(),

            // Pending Assessments (filtered by target users)
            PendingAssessmentCount = await _context.AssessmentSessions
                .Where(a => targetUserIds.Contains(a.UserId) &&
                       a.Status != "Completed")
                .CountAsync(),

            // Mandatory Training (personal only - always current user)
            MandatoryTrainingStatus = await GetMandatoryTrainingStatus(user.Id),

            // Recent Activities (filtered by target users)
            RecentActivities = await GetRecentActivities(targetUserIds),

            // Upcoming Deadlines (filtered by target users)
            UpcomingDeadlines = await GetUpcomingDeadlines(targetUserIds)
        };

        // Calculate IDP percentage
        viewModel.IdpProgressPercentage = viewModel.IdpTotalCount > 0
            ? (int)((double)viewModel.IdpCompletedCount / viewModel.IdpTotalCount * 100)
            : 0;

        // Check urgency (assessments due within 3 days)
        viewModel.HasUrgentAssessments = await _context.AssessmentSessions
            .AnyAsync(a => a.UserId == user.Id &&
                     a.Status != "Completed" &&
                     a.Schedule <= DateTime.Now.AddDays(3));

        return View(viewModel);
    }

    private string GetTimeBasedGreeting()
    {
        var hour = DateTime.Now.Hour;
        return hour < 12 ? "Selamat Pagi"
             : hour < 15 ? "Selamat Siang"
             : hour < 18 ? "Selamat Sore"
             : "Selamat Malam";
    }

    private async Task<TrainingStatusInfo> GetMandatoryTrainingStatus(string userId)
    {
        var hsseTraining = await _context.TrainingRecords
            .Where(t => t.UserId == userId &&
                   t.Kategori == "MANDATORY" &&
                   t.Status == "Valid")
            .OrderByDescending(t => t.ValidUntil)
            .FirstOrDefaultAsync();

        if (hsseTraining == null || !hsseTraining.ValidUntil.HasValue)
        {
            return new TrainingStatusInfo
            {
                IsValid = false,
                Status = "NO RECORDS",
                DaysUntilExpiry = 0
            };
        }

        var daysRemaining = (hsseTraining.ValidUntil.Value - DateTime.Now).Days;

        return new TrainingStatusInfo
        {
            IsValid = daysRemaining > 0,
            Status = daysRemaining > 30 ? "VALID"
                   : daysRemaining > 0 ? "EXPIRING SOON"
                   : "EXPIRED",
            ValidUntil = hsseTraining.ValidUntil,
            CertificateUrl = hsseTraining.SertifikatUrl,
            DaysUntilExpiry = daysRemaining
        };
    }

    private async Task<List<RecentActivityItem>> GetRecentActivities(List<string> userIds)
    {
        var activities = new List<RecentActivityItem>();

        // Recent Assessment Completions
        var completedAssessments = await _context.AssessmentSessions
            .Where(a => userIds.Contains(a.UserId) && a.Status == "Completed")
            .OrderByDescending(a => a.Schedule)
            .Take(2)
            .ToListAsync();

        activities.AddRange(completedAssessments.Select(a => new RecentActivityItem
        {
            Title = "Completed Assessment",
            Description = a.Title,
            Timestamp = a.Schedule,
            TimeAgo = GetTimeAgo(a.Schedule),
            IconClass = "fas fa-clipboard-check"
        }));

        // Recent IDP Updates (use DueDate as proxy for completion date)
        var recentIdp = await _context.IdpItems
            .Where(i => userIds.Contains(i.UserId) &&
                   (i.Status == "Completed" || i.Status == "Approved"))
            .OrderByDescending(i => i.DueDate)
            .Take(2)
            .ToListAsync();

        activities.AddRange(recentIdp.Select(i => new RecentActivityItem
        {
            Title = "Updated IDP Progress",
            Description = $"Marked \"{i.Kompetensi}\" as completed",
            Timestamp = i.DueDate,
            TimeAgo = GetTimeAgo(i.DueDate),
            IconClass = "fas fa-tasks"
        }));

        // Recent Coaching Sessions
        var recentCoaching = await _context.CoachingLogs
            .Where(c => userIds.Contains(c.CoacheeId) && c.Status == "Submitted")
            .OrderByDescending(c => c.Tanggal)
            .Take(2)
            .ToListAsync();

        activities.AddRange(recentCoaching.Select(c => new RecentActivityItem
        {
            Title = "Coaching Session",
            Description = $"Session with {c.CoachName}",
            Timestamp = c.Tanggal,
            TimeAgo = GetTimeAgo(c.Tanggal),
            IconClass = "fas fa-users"
        }));

        // Sort all activities by timestamp and take top 4
        return activities
            .OrderByDescending(a => a.Timestamp)
            .Take(4)
            .ToList();
    }

    private async Task<List<DeadlineItem>> GetUpcomingDeadlines(List<string> userIds)
    {
        var deadlines = new List<DeadlineItem>();
        var now = DateTime.Now;

        // Open Assessments
        var openAssessments = await _context.AssessmentSessions
            .Where(a => userIds.Contains(a.UserId) &&
                   a.Status != "Completed" &&
                   a.Schedule >= now)
            .OrderBy(a => a.Schedule)
            .Take(3)
            .ToListAsync();

        deadlines.AddRange(openAssessments.Select(a => new DeadlineItem
        {
            Title = $"Submit {a.Title}",
            DueDate = a.Schedule,
            DueDateFormatted = a.Schedule.ToString("dd MMMM yyyy"),
            DaysRemaining = (a.Schedule - now).Days,
            UrgencyClass = (a.Schedule - now).Days <= 3 ? "urgent" : "normal",
            IconClass = "fas fa-exclamation",
            ActionUrl = $"/CMP/StartExam/{a.Id}"
        }));

        // Pending IDP Items
        var pendingIdp = await _context.IdpItems
            .Where(i => userIds.Contains(i.UserId) &&
                   i.Status != "Completed" &&
                   i.DueDate >= now)
            .OrderBy(i => i.DueDate)
            .Take(2)
            .ToListAsync();

        deadlines.AddRange(pendingIdp.Select(i => new DeadlineItem
        {
            Title = $"Complete IDP: {i.Kompetensi}",
            DueDate = i.DueDate,
            DueDateFormatted = i.DueDate.ToString("dd MMMM yyyy"),
            DaysRemaining = (i.DueDate - now).Days,
            UrgencyClass = (i.DueDate - now).Days <= 7 ? "urgent" : "normal",
            IconClass = "fas fa-file-alt",
            ActionUrl = "/CDP/Index"
        }));

        // Expiring Certifications (Personal only - single user from first ID)
        if (userIds.Count == 1)
        {
            var expiringCerts = await _context.TrainingRecords
                .Where(t => t.UserId == userIds[0] &&
                       t.ValidUntil.HasValue &&
                       t.ValidUntil.Value >= now &&
                       t.Status == "Valid")
                .OrderBy(t => t.ValidUntil)
                .Take(2)
                .ToListAsync();

            deadlines.AddRange(expiringCerts.Select(t => new DeadlineItem
            {
                Title = $"Renew {t.Judul} Certification",
                DueDate = t.ValidUntil!.Value,
                DueDateFormatted = t.ValidUntil.Value.ToString("dd MMMM yyyy"),
                DaysRemaining = (t.ValidUntil.Value - now).Days,
                UrgencyClass = (t.ValidUntil.Value - now).Days <= 30 ? "urgent" : "normal",
                IconClass = "fas fa-graduation-cap",
                ActionUrl = "/CMP/Records"
            }));
        }

        // Sort by urgency then date, return top 4
        return deadlines
            .OrderBy(d => d.DaysRemaining)
            .Take(4)
            .ToList();
    }

    private string GetTimeAgo(DateTime timestamp)
    {
        var span = DateTime.Now - timestamp;

        if (span.TotalMinutes < 60)
            return $"{(int)span.TotalMinutes} minutes ago";
        if (span.TotalHours < 24)
            return $"{(int)span.TotalHours} hours ago";
        if (span.TotalDays < 7)
            return $"{(int)span.TotalDays} days ago";
        if (span.TotalDays < 30)
            return $"{(int)(span.TotalDays / 7)} weeks ago";

        return timestamp.ToString("dd MMM yyyy");
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
