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
    public int GradedCount { get; set; }   // Phase 345 CMP06R-02 D-05: sesi IsPassed != null (denominator passRate)
    public int PendingCount { get; set; }  // Phase 345 CMP06R-02 D-06: sesi IsPassed == null (indikator "Menunggu Penilaian: N")
    public List<AssessmentReportItem> Assessments { get; set; } = new();
}
