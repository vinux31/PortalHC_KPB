namespace HcPortal.Models.Competency;

public class CpdpProgressViewModel
{
    public string UserId { get; set; } = "";
    public string UserName { get; set; } = "";
    public string? Position { get; set; }
    public string? Section { get; set; }

    public List<CpdpProgressItem> Items { get; set; } = new();

    // Summary
    public int TotalCpdpItems { get; set; }
    public int ItemsWithEvidence { get; set; }
    public double EvidenceCoverage { get; set; } // Percentage of CPDP items with at least one assessment
}

public class CpdpProgressItem
{
    public int CpdpItemId { get; set; }
    public string No { get; set; } = "";
    public string NamaKompetensi { get; set; } = "";
    public string IndikatorPerilaku { get; set; } = "";
    public string Silabus { get; set; } = "";
    public string TargetDeliverable { get; set; } = "";
    public string CpdpStatus { get; set; } = ""; // Original CPDP status

    // Competency tracking
    public int? CurrentLevel { get; set; }
    public int? TargetLevel { get; set; }
    public string CompetencyStatus { get; set; } = "Not Tracked"; // "Met", "Gap", "Not Started", "Not Tracked"

    // Assessment evidence
    public List<AssessmentEvidence> Evidences { get; set; } = new();

    // IDP activity
    public bool HasIdpActivity { get; set; }
    public string? IdpStatus { get; set; }
}

public class AssessmentEvidence
{
    public int AssessmentSessionId { get; set; }
    public string Title { get; set; } = "";
    public string Category { get; set; } = "";
    public int? Score { get; set; }
    public bool? IsPassed { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int LevelGranted { get; set; }
}
