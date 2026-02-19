namespace HcPortal.Models;

public class DevDashboardViewModel
{
    // Summary cards
    public int TotalCoachees { get; set; }
    public int TotalDeliverables { get; set; }
    public int ApprovedDeliverables { get; set; }
    public int PendingSpvApprovals { get; set; }    // Status == "Submitted"
    public int PendingHCReviews { get; set; }       // HCApprovalStatus == "Pending" && Status == "Approved"
    public int CompletedCoachees { get; set; }      // coachees with a ProtonFinalAssessment

    // Per-coachee rows — CoacheeProgressRow now lives in CDPDashboardViewModel.cs
    public List<CoacheeProgressRow> CoacheeRows { get; set; } = new();

    // Chart: competency level granted trend (line chart)
    public List<string> TrendLabels { get; set; } = new();   // e.g. ["2025-10", "2025-11"]
    public List<double> TrendValues { get; set; } = new();   // avg CompetencyLevelGranted per month

    // Chart: deliverable status distribution (doughnut)
    // Labels: ["Approved", "Submitted", "Active", "Rejected", "Locked"]
    public List<string> StatusLabels { get; set; } = new();
    public List<int> StatusData { get; set; } = new();

    // Context display
    public string CurrentUserRole { get; set; } = "";
    public string ScopeLabel { get; set; } = "";   // "Unit: RFCC NHT" / "Section: GAST" / "All Sections"
}
