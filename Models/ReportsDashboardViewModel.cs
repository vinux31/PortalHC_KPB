namespace HcPortal.Models;

public class ReportsDashboardViewModel
{
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

public class AssessmentReportItem
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Category { get; set; } = "";
    public string UserId { get; set; } = "";
    public string UserName { get; set; } = "";
    public string? UserNIP { get; set; }
    public string? UserSection { get; set; }
    public int Score { get; set; }
    public int PassPercentage { get; set; }
    public bool IsPassed { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class ReportFilters
{
    public string? Category { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Section { get; set; }
    public string? UserSearch { get; set; }
}

public class CategoryStatistic
{
    public string CategoryName { get; set; } = "";
    public int TotalAssessments { get; set; }
    public int PassedCount { get; set; }
    public double PassRate { get; set; }
    public double AverageScore { get; set; }
}
