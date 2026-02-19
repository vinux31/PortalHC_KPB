namespace HcPortal.Models;

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
