using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using HcPortal.Models;
using HcPortal.Data;
using HcPortal.Services;

namespace HcPortal.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly ILogger<HomeController> _logger;
    private readonly IMemoryCache _cache;

    public HomeController(UserManager<ApplicationUser> userManager, ApplicationDbContext context,
        INotificationService notificationService, ILogger<HomeController> logger, IMemoryCache cache)
    {
        _userManager = userManager;
        _context = context;
        _notificationService = notificationService;
        _logger = logger;
        _cache = cache;
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

        if (User.IsInRole("HC") || User.IsInRole("Admin"))
        {
            var (expiredCount, akanExpiredCount) = await GetCertAlertCountsAsync();
            viewModel.ExpiredCount = expiredCount;
            viewModel.AkanExpiredCount = akanExpiredCount;
            var cacheKey = "cert-notif-global";
            if (!_cache.TryGetValue(cacheKey, out _))
            {
                await TriggerCertExpiredNotificationsAsync();
                _cache.Set(cacheKey, true, TimeSpan.FromHours(1));
            }
        }

        return View(viewModel);
    }

    private async Task TriggerCertExpiredNotificationsAsync()
    {
        try
        {
            var today = DateTime.Today;

            // Renewal chain resolution (same as GetCertAlertCountsAsync)
            var renewedByAsSessionIds = await _context.AssessmentSessions
                .Where(a => a.RenewsSessionId.HasValue && a.IsPassed == true)
                .Select(a => a.RenewsSessionId!.Value).Distinct().ToListAsync();
            var renewedByTrSessionIds = await _context.TrainingRecords
                .Where(t => t.RenewsSessionId.HasValue)
                .Select(t => t.RenewsSessionId!.Value).Distinct().ToListAsync();
            var renewedByAsTrainingIds = await _context.AssessmentSessions
                .Where(a => a.RenewsTrainingId.HasValue && a.IsPassed == true)
                .Select(a => a.RenewsTrainingId!.Value).Distinct().ToListAsync();
            var renewedByTrTrainingIds = await _context.TrainingRecords
                .Where(t => t.RenewsTrainingId.HasValue)
                .Select(t => t.RenewsTrainingId!.Value).Distinct().ToListAsync();

            var renewedSessionIds = new HashSet<int>(renewedByAsSessionIds);
            renewedSessionIds.UnionWith(renewedByTrSessionIds);
            var renewedTrainingIds = new HashSet<int>(renewedByAsTrainingIds);
            renewedTrainingIds.UnionWith(renewedByTrTrainingIds);

            // Expired TrainingRecords
            var expiredTrainings = await _context.TrainingRecords
                .Include(t => t.User)
                .Where(t => t.SertifikatUrl != null && t.CertificateType != "Permanent"
                    && t.ValidUntil.HasValue && t.ValidUntil.Value < today)
                .Select(t => new { t.Id, Judul = t.Judul ?? "", NamaWorker = t.User != null ? t.User.FullName : "" })
                .ToListAsync();

            // Expired AssessmentSessions
            var expiredAssessments = await _context.AssessmentSessions
                .Include(a => a.User)
                .Where(a => a.GenerateCertificate && a.IsPassed == true
                    && a.ValidUntil.HasValue && a.ValidUntil.Value < today)
                .Select(a => new { a.Id, Judul = a.Title, NamaWorker = a.User != null ? a.User.FullName : "" })
                .ToListAsync();

            // Filter out renewed, build message list
            var expiredCerts = expiredTrainings
                .Where(t => !renewedTrainingIds.Contains(t.Id))
                .Select(t => new { t.Judul, t.NamaWorker })
                .Concat(expiredAssessments
                    .Where(a => !renewedSessionIds.Contains(a.Id))
                    .Select(a => new { a.Judul, a.NamaWorker }))
                .ToList();

            if (expiredCerts.Count == 0) return;

            var hcUsers = await _userManager.GetUsersInRoleAsync("HC");
            var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
            var targetUsers = hcUsers.Concat(adminUsers).DistinctBy(u => u.Id).ToList();

            // Pre-fetch existing CERT_EXPIRED notifications for dedup
            var existingNotifications = await _context.UserNotifications
                .Where(n => n.Type == "CERT_EXPIRED")
                .Select(n => new { n.UserId, n.Message })
                .ToListAsync();
            var existingSet = new HashSet<string>(existingNotifications.Select(n => $"{n.UserId}|{n.Message}"));

            foreach (var cert in expiredCerts)
            {
                var message = $"Sertifikat {cert.Judul} milik {cert.NamaWorker} telah expired";
                foreach (var targetUser in targetUsers)
                {
                    if (existingSet.Contains($"{targetUser.Id}|{message}"))
                        continue;

                    await _notificationService.SendAsync(
                        targetUser.Id,
                        "CERT_EXPIRED",
                        "Sertifikat Expired",
                        message,
                        "/Admin/RenewalCertificate"
                    );
                    existingSet.Add($"{targetUser.Id}|{message}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to trigger CERT_EXPIRED notifications");
        }
    }

    private async Task<(int expiredCount, int akanExpiredCount)> GetCertAlertCountsAsync()
    {
        var today = DateTime.Today;
        var thirtyDaysFromNow = today.AddDays(30);

        // Renewal chain resolution: batch lookup (same pattern as AdminController.BuildRenewalRowsAsync)
        var renewedByAsSessionIds = await _context.AssessmentSessions
            .Where(a => a.RenewsSessionId.HasValue && a.IsPassed == true)
            .Select(a => a.RenewsSessionId!.Value)
            .Distinct().ToListAsync();
        var renewedByTrSessionIds = await _context.TrainingRecords
            .Where(t => t.RenewsSessionId.HasValue)
            .Select(t => t.RenewsSessionId!.Value)
            .Distinct().ToListAsync();
        var renewedByAsTrainingIds = await _context.AssessmentSessions
            .Where(a => a.RenewsTrainingId.HasValue && a.IsPassed == true)
            .Select(a => a.RenewsTrainingId!.Value)
            .Distinct().ToListAsync();
        var renewedByTrTrainingIds = await _context.TrainingRecords
            .Where(t => t.RenewsTrainingId.HasValue)
            .Select(t => t.RenewsTrainingId!.Value)
            .Distinct().ToListAsync();

        var renewedSessionIds = new HashSet<int>(renewedByAsSessionIds);
        renewedSessionIds.UnionWith(renewedByTrSessionIds);
        var renewedTrainingIds = new HashSet<int>(renewedByAsTrainingIds);
        renewedTrainingIds.UnionWith(renewedByTrTrainingIds);

        // TrainingRecords with certificate, non-Permanent, has ValidUntil
        var trainingCerts = await _context.TrainingRecords
            .Where(t => t.SertifikatUrl != null && t.CertificateType != "Permanent" && t.ValidUntil.HasValue)
            .Select(t => new { t.Id, t.ValidUntil })
            .ToListAsync();

        int trExpired = trainingCerts.Count(t => t.ValidUntil!.Value < today && !renewedTrainingIds.Contains(t.Id));
        int trAkanExpired = trainingCerts.Count(t => t.ValidUntil!.Value >= today && t.ValidUntil.Value <= thirtyDaysFromNow && !renewedTrainingIds.Contains(t.Id));

        // AssessmentSessions with certificate
        var assessmentCerts = await _context.AssessmentSessions
            .Where(a => a.GenerateCertificate && a.IsPassed == true && a.ValidUntil.HasValue)
            .Select(a => new { a.Id, a.ValidUntil })
            .ToListAsync();

        int asExpired = assessmentCerts.Count(a => a.ValidUntil!.Value < today && !renewedSessionIds.Contains(a.Id));
        int asAkanExpired = assessmentCerts.Count(a => a.ValidUntil!.Value >= today && a.ValidUntil.Value <= thirtyDaysFromNow && !renewedSessionIds.Contains(a.Id));

        return (trExpired + asExpired, trAkanExpired + asAkanExpired);
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

        // Normalize module to lowercase so /Home/GuideDetail?module=CMP works same as ?module=cmp
        module = module?.ToLowerInvariant() ?? "";

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
