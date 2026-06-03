namespace HcPortal.Models
{
    /// <summary>
    /// Label tampilan tier organisasi (Phase 340 — milestone v21.0).
    /// Level adalah natural PK. Label di-CRUD oleh HC/Admin via /Admin/ManageOrgLevelLabels (Phase 341).
    /// </summary>
    public class OrganizationLevelLabel
    {
        public int Level { get; set; }
        public string Label { get; set; } = "";
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string UpdatedBy { get; set; } = "";
    }
}
