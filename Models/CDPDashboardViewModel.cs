namespace HcPortal.Models;

// ============================================================
// Wrapper ViewModel — Dashboard() serves all roles via sub-models
// ============================================================
public class CDPDashboardViewModel
{
    public string CurrentUserRole { get; set; } = "";
    public string ScopeLabel { get; set; } = "";

    // Populated for literal Coachee role only (early return in Dashboard())
    public CoacheeDashboardSubModel? CoacheeData { get; set; }

    // Populated for all non-Coachee roles (HC/Admin/Coach/Supervisor)
    public ProtonProgressSubModel? ProtonProgressData { get; set; }

    // Populated for HC and Admin regardless of SelectedView
    public AssessmentAnalyticsSubModel? AssessmentAnalyticsData { get; set; }
}

// ============================================================
// Sub-model 1: Coachee's personal deliverable view
// ============================================================
public class CoacheeDashboardSubModel
{
    public string TrackType { get; set; } = "";        // "Panelman" / "Operator"
    public string TahunKe { get; set; } = "";          // "1" / "2" / "3"
    public int TotalDeliverables { get; set; }
    public int ApprovedDeliverables { get; set; }
    public int ActiveDeliverables { get; set; }
    public int? CompetencyLevelGranted { get; set; }   // from ProtonFinalAssessment
    public string CurrentStatus { get; set; } = "";    // "In Progress" / "Completed"
}

// ============================================================
// Sub-model 2: Supervisor / HC Proton Progress view
// ============================================================
public class ProtonProgressSubModel
{
    // Stat card fields
    public int TotalCoachees { get; set; }
    public int TotalDeliverables { get; set; }
    public int ApprovedDeliverables { get; set; }
    public int PendingSpvApprovals { get; set; }    // Status == "Submitted"
    public int PendingHCReviews { get; set; }       // HCApprovalStatus == "Pending" && Status == "Approved"
    public int CompletedCoachees { get; set; }      // coachees with a ProtonFinalAssessment

    // Per-coachee flat table (sorted by name)
    public List<CoacheeProgressRow> CoacheeRows { get; set; } = new();

    // Chart: competency level granted trend (line chart)
    public List<string> TrendLabels { get; set; } = new();   // e.g. ["2025-10", "2025-11"]
    public List<double> TrendValues { get; set; } = new();   // avg CompetencyLevelGranted per month

    // Chart: deliverable status distribution (doughnut)
    public List<string> StatusLabels { get; set; } = new();
    public List<int> StatusData { get; set; } = new();
}

// ============================================================
// Sub-model 3: HC / Admin Assessment Analytics view
// ============================================================
public class AssessmentAnalyticsSubModel
{
    // KPI card fields
    public int TotalAssigned { get; set; }
    public int TotalCompleted { get; set; }
    public int PassedCount { get; set; }
    public double PassRate { get; set; }
    public double AverageScore { get; set; }

    // Paginated assessment table
    public List<AssessmentReportItem> Assessments { get; set; } = new();
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public int PageSize { get; set; }

    // Filters
    public ReportFilters CurrentFilters { get; set; } = new();
    public List<string> AvailableCategories { get; set; } = new();
    public List<string> AvailableSections { get; set; } = new();

    // Chart data
    public List<CategoryStatistic> CategoryStats { get; set; } = new();
    public List<int> ScoreDistribution { get; set; } = new();
}

// ============================================================
// Supporting classes (copied from DevDashboardViewModel + ReportsDashboardViewModel)
// These classes are self-contained here; DevDashboardViewModel and
// ReportsDashboardViewModel will be deleted in Plan 12-03.
// ============================================================

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
