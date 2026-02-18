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

    // Per-coachee rows
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

public class CoacheeProgressRow
{
    public string CoacheeId { get; set; } = "";
    public string CoacheeName { get; set; } = "";
    public string TrackType { get; set; } = "";     // "Operator" or "Panelman"; "" if not assigned
    public string TahunKe { get; set; } = "";       // "Tahun 1" / "Tahun 2" / "Tahun 3"; "" if not assigned
    public int TotalDeliverables { get; set; }
    public int Approved { get; set; }
    public int Submitted { get; set; }
    public int Rejected { get; set; }
    public int Active { get; set; }
    public int Locked { get; set; }
    public bool HasFinalAssessment { get; set; }
    public int? CompetencyLevelGranted { get; set; }

    public string ProgressPercent => TotalDeliverables > 0
        ? $"{(int)((double)Approved / TotalDeliverables * 100)}%"
        : "0%";
}
