namespace HcPortal.Models;

/// <summary>
/// Master table — top level of Proton deliverable hierarchy
/// </summary>
public class ProtonKompetensi
{
    public int Id { get; set; }
    public string NamaKompetensi { get; set; } = "";
    /// <summary>Values: "Panelman" or "Operator"</summary>
    public string TrackType { get; set; } = "";
    /// <summary>Values: "Tahun 1", "Tahun 2", "Tahun 3"</summary>
    public string TahunKe { get; set; } = "";
    public int Urutan { get; set; }

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
    /// <summary>Values: "Panelman" or "Operator"</summary>
    public string TrackType { get; set; } = "";
    /// <summary>Values: "Tahun 1", "Tahun 2", "Tahun 3"</summary>
    public string TahunKe { get; set; } = "";
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
    /// <summary>Values: "Locked", "Active", "Submitted", "Approved", "Rejected"</summary>
    public string Status { get; set; } = "Locked";
    /// <summary>Relative web path like "/uploads/evidence/{id}/{filename}"</summary>
    public string? EvidencePath { get; set; }
    /// <summary>Original display name of uploaded file</summary>
    public string? EvidenceFileName { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? RejectedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
