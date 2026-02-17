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
}
