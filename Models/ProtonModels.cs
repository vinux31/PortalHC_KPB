namespace HcPortal.Models;

/// <summary>
/// Normalized track entity — single source of truth for Proton track identifiers (Phase 33)
/// </summary>
public class ProtonTrack
{
    public int Id { get; set; }
    /// <summary>Values: "Panelman" or "Operator"</summary>
    public string TrackType { get; set; } = "";
    /// <summary>Values: "Tahun 1", "Tahun 2", "Tahun 3"</summary>
    public string TahunKe { get; set; } = "";
    /// <summary>Auto-generated at seed time. Format: "Panelman - Tahun 1" (TrackType + " - " + TahunKe)</summary>
    public string DisplayName { get; set; } = "";
    /// <summary>Display order in UI dropdowns (1-6)</summary>
    public int Urutan { get; set; }

    // Navigation property — one Track has many Kompetensi
    public ICollection<ProtonKompetensi> KompetensiList { get; set; } = new List<ProtonKompetensi>();
}

/// <summary>
/// Master table — top level of Proton deliverable hierarchy
/// </summary>
public class ProtonKompetensi
{
    public int Id { get; set; }
    public string Bagian { get; set; } = "";
    public string Unit { get; set; } = "";
    public string NamaKompetensi { get; set; } = "";
    public int Urutan { get; set; }
    public int ProtonTrackId { get; set; }
    public ProtonTrack? ProtonTrack { get; set; }

    public ICollection<ProtonSubKompetensi> SubKompetensiList { get; set; } = new List<ProtonSubKompetensi>();
}

/// <summary>
/// Master table — mid level of Proton deliverable hierarchy
/// </summary>
public class ProtonSubKompetensi
{
    public int Id { get; set; }
    public int ProtonKompetensiId { get; set; }
    public ProtonKompetensi? ProtonKompetensi { get; set; }
    public string NamaSubKompetensi { get; set; } = "";
    public int Urutan { get; set; }

    public ICollection<ProtonDeliverable> Deliverables { get; set; } = new List<ProtonDeliverable>();
}

/// <summary>
/// Master table — leaf level of Proton deliverable hierarchy
/// </summary>
public class ProtonDeliverable
{
    public int Id { get; set; }
    public int ProtonSubKompetensiId { get; set; }
    public ProtonSubKompetensi? ProtonSubKompetensi { get; set; }
    public string NamaDeliverable { get; set; } = "";
    public int Urutan { get; set; }
}

/// <summary>
/// Per-user assignment to a Proton track (e.g. Operator Tahun 1)
/// </summary>
public class ProtonTrackAssignment
{
    public int Id { get; set; }
    /// <summary>No FK constraint — matches CoachingLog/CoachCoacheeMapping pattern</summary>
    public string CoacheeId { get; set; } = "";
    /// <summary>No FK constraint</summary>
    public string AssignedById { get; set; } = "";
    public int ProtonTrackId { get; set; }
    public ProtonTrack? ProtonTrack { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Per-user per-deliverable progress tracking
/// </summary>
public class ProtonDeliverableProgress
{
    public int Id { get; set; }
    /// <summary>No FK constraint — matches CoachingLog/CoachCoacheeMapping pattern</summary>
    public string CoacheeId { get; set; } = "";
    public int ProtonDeliverableId { get; set; }
    public ProtonDeliverable? ProtonDeliverable { get; set; }
    /// <summary>Values: "Pending", "Submitted", "Approved", "Rejected"</summary>
    public string Status { get; set; } = "Pending";
    /// <summary>Relative web path like "/uploads/evidence/{id}/{filename}"</summary>
    public string? EvidencePath { get; set; }
    /// <summary>Original display name of uploaded file</summary>
    public string? EvidenceFileName { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? RejectedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ===== Phase 6: Approval Workflow =====
    /// <summary>Written rejection reason (APPRV-05). Set by SrSpv or SectionHead.</summary>
    public string? RejectionReason { get; set; }

    /// <summary>User ID of the approver who approved. No FK — matches pattern.</summary>
    public string? ApprovedById { get; set; }

    /// <summary>HC review channel — independent of main Status. "Pending" or "Reviewed". (APPRV-04)</summary>
    public string HCApprovalStatus { get; set; } = "Pending";

    /// <summary>When HC marked as reviewed.</summary>
    public DateTime? HCReviewedAt { get; set; }

    /// <summary>HC user ID who reviewed. No FK — matches pattern.</summary>
    public string? HCReviewedById { get; set; }

    // ===== Phase 65: Independent Per-Role Approval =====
    /// <summary>SrSpv independent approval. Values: "Pending", "Approved", "Rejected"</summary>
    public string SrSpvApprovalStatus { get; set; } = "Pending";
    /// <summary>SrSpv approver user ID. No FK — matches pattern.</summary>
    public string? SrSpvApprovedById { get; set; }
    /// <summary>When SrSpv approved/rejected.</summary>
    public DateTime? SrSpvApprovedAt { get; set; }

    /// <summary>SectionHead independent approval. Values: "Pending", "Approved", "Rejected"</summary>
    public string ShApprovalStatus { get; set; } = "Pending";
    /// <summary>SectionHead approver user ID. No FK — matches pattern.</summary>
    public string? ShApprovedById { get; set; }
    /// <summary>When SectionHead approved/rejected.</summary>
    public DateTime? ShApprovedAt { get; set; }
}

/// <summary>
/// In-app notification for HC when coachee completes all deliverables (PROTN-06)
/// </summary>
public class ProtonNotification
{
    public int Id { get; set; }
    /// <summary>HC user ID who receives the notification. No FK — matches CoachingLog pattern.</summary>
    public string RecipientId { get; set; } = "";
    /// <summary>Coachee user ID who triggered the notification. No FK.</summary>
    public string CoacheeId { get; set; } = "";
    /// <summary>Denormalized coachee display name for display without extra query.</summary>
    public string CoacheeName { get; set; } = "";
    public string Message { get; set; } = "";
    /// <summary>Values: "AllDeliverablesComplete"</summary>
    public string Type { get; set; } = "AllDeliverablesComplete";
    public bool IsRead { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReadAt { get; set; }
}

/// <summary>
/// Coaching guidance file — learning materials (PDF, Word, Excel, PPT) per Bagian+Unit+Track
/// </summary>
public class CoachingGuidanceFile
{
    public int Id { get; set; }
    public string Bagian { get; set; } = "";
    public string Unit { get; set; } = "";
    public int ProtonTrackId { get; set; }
    public ProtonTrack? ProtonTrack { get; set; }
    public string FileName { get; set; } = "";       // Original display name
    public string FilePath { get; set; } = "";       // Web-relative path e.g. /uploads/guidance/...
    public long FileSize { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public string UploadedById { get; set; } = "";
}

/// <summary>
/// Final Proton Assessment record created by HC after all deliverables reviewed (PROTN-07)
/// </summary>
public class ProtonFinalAssessment
{
    public int Id { get; set; }
    /// <summary>No FK constraint — matches pattern.</summary>
    public string CoacheeId { get; set; } = "";
    /// <summary>HC user ID who created the assessment. No FK.</summary>
    public string CreatedById { get; set; } = "";
    public int ProtonTrackAssignmentId { get; set; }
    public ProtonTrackAssignment? ProtonTrackAssignment { get; set; }
    /// <summary>Values: "Completed"</summary>
    public string Status { get; set; } = "Completed";
    /// <summary>Competency level granted by HC (0-5)</summary>
    public int CompetencyLevelGranted { get; set; }
    /// <summary>Nullable — HC selects competency from KKJ dropdown. No nav property to avoid cascade conflicts.</summary>
    public int? KkjMatrixItemId { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}
