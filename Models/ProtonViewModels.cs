using HcPortal.Models.Competency;

namespace HcPortal.Models;

/// <summary>
/// ViewModel for ProtonMain page (PROTN-01) — coach views coachees and their track assignments
/// </summary>
public class ProtonMainViewModel
{
    /// <summary>Coachees in the coach's section</summary>
    public List<ApplicationUser> Coachees { get; set; } = new();

    /// <summary>Existing active track assignments for coachees</summary>
    public List<ProtonTrackAssignment> Assignments { get; set; } = new();

    /// <summary>
    /// Active progress records for each coachee (Status == "Active").
    /// Used to render "Lihat Deliverable" link buttons per coachee row.
    /// </summary>
    public List<ProtonDeliverableProgress> ActiveProgresses { get; set; } = new();
}

/// <summary>
/// ViewModel for PlanIdp Coachee view (PROTN-02) — coachee views their full deliverable plan
/// </summary>
public class ProtonPlanViewModel
{
    public string TrackType { get; set; } = "";
    public string TahunKe { get; set; } = "";

    /// <summary>Full Kompetensi > SubKompetensi > Deliverable hierarchy for the assigned track</summary>
    public List<ProtonKompetensi> KompetensiList { get; set; } = new();

    /// <summary>
    /// The coachee's current active deliverable progress record (Status == "Active").
    /// Used to render a navigation button to the Deliverable detail page.
    /// </summary>
    public ProtonDeliverableProgress? ActiveProgress { get; set; }

    /// <summary>Final assessment record for this coachee's track. Null if no assessment exists yet (PROTN-08).</summary>
    public ProtonFinalAssessment? FinalAssessment { get; set; }
}

/// <summary>
/// ViewModel for Deliverable detail page (PROTN-04/05) — coachee submits or reviews evidence
/// </summary>
public class DeliverableViewModel
{
    public ProtonDeliverableProgress? Progress { get; set; }
    public ProtonDeliverable? Deliverable { get; set; }
    public string CoacheeName { get; set; } = "";
    public string TrackType { get; set; } = "";
    public string TahunKe { get; set; } = "";

    /// <summary>Computed from sequential lock logic — whether this deliverable is accessible</summary>
    public bool IsAccessible { get; set; }

    /// <summary>True if Status is "Active" or "Rejected" — coachee can upload evidence</summary>
    public bool CanUpload { get; set; }

    // ===== Phase 6: Approval Workflow =====
    /// <summary>True for SrSpv/SectionHead when Status=="Submitted" — can approve or reject</summary>
    public bool CanApprove { get; set; }

    /// <summary>True for HC when HCApprovalStatus=="Pending" — can mark as HC reviewed</summary>
    public bool CanHCReview { get; set; }

    /// <summary>Current user's role — for conditional view rendering</summary>
    public string CurrentUserRole { get; set; } = "";
}

/// <summary>
/// ViewModel for HC approval queue page — HC views pending reviews, notifications, and candidates for final assessment
/// </summary>
public class HCApprovalQueueViewModel
{
    /// <summary>Deliverable progress records with HCApprovalStatus=="Pending"</summary>
    public List<ProtonDeliverableProgress> PendingReviews { get; set; } = new();

    /// <summary>Unread notifications for this HC</summary>
    public List<ProtonNotification> Notifications { get; set; } = new();

    /// <summary>Coachee display names keyed by user ID — avoids N+1 queries</summary>
    public Dictionary<string, string> UserNames { get; set; } = new();

    /// <summary>Coachees who have completed all deliverables and are ready for final assessment</summary>
    public List<FinalAssessmentCandidate> ReadyForFinalAssessment { get; set; } = new();
}

/// <summary>
/// Simple DTO representing a coachee ready for their final Proton assessment
/// </summary>
public class FinalAssessmentCandidate
{
    public string CoacheeId { get; set; } = "";
    public string CoacheeName { get; set; } = "";
    public int TrackAssignmentId { get; set; }
    public string TrackType { get; set; } = "";
    public string TahunKe { get; set; } = "";
}

/// <summary>
/// ViewModel for the final assessment creation page — HC submits final Proton assessment
/// </summary>
public class FinalAssessmentViewModel
{
    public ProtonTrackAssignment? Assignment { get; set; }
    public string CoacheeName { get; set; } = "";
    public int TotalDeliverables { get; set; }
    public int ApprovedDeliverables { get; set; }
    public bool AllHCReviewed { get; set; }

    /// <summary>KKJ competency items for HC to select from dropdown</summary>
    public List<KkjMatrixItem> AvailableCompetencies { get; set; } = new();

    /// <summary>Null if no final assessment has been created yet</summary>
    public ProtonFinalAssessment? ExistingAssessment { get; set; }
}
