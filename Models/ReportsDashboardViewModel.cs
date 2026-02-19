namespace HcPortal.Models;

public class ReportsDashboardViewModel
{
    // AssessmentReportItem, ReportFilters, CategoryStatistic now live in CDPDashboardViewModel.cs
    public List<AssessmentReportItem> Assessments { get; set; } = new();
    public int TotalAssessments { get; set; }
    public int PassedCount { get; set; }
    public double PassRate { get; set; }
    public double AverageScore { get; set; }
    public int TotalAssigned { get; set; }
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public int PageSize { get; set; }
    public ReportFilters CurrentFilters { get; set; } = new();
    public List<string> AvailableCategories { get; set; } = new();
    public List<string> AvailableSections { get; set; } = new();
}

public class UserAssessmentHistoryViewModel
{
    public string UserId { get; set; } = "";
    public string UserFullName { get; set; } = "";
    public string? UserNIP { get; set; }
    public string? UserSection { get; set; }
    public string? UserPosition { get; set; }
    public int TotalAssessments { get; set; }
    public int PassedCount { get; set; }
    public double PassRate { get; set; }
    public double AverageScore { get; set; }
    public List<AssessmentReportItem> Assessments { get; set; } = new();
}
