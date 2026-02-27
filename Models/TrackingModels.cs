namespace HcPortal.Models
{
    public class TrackingItem
    {
        public int Id { get; set; }
        public string Kompetensi { get; set; } = "";
        public string Periode { get; set; } = ""; // e.g. "Tahun Pertama"
        public string SubKompetensi { get; set; } = "";
        public string Deliverable { get; set; } = "";
        public string EvidenceStatus { get; set; } = ""; // "Uploaded", "Pending", "-"
        public string FullEvidencePath { get; set; } = ""; 
        public string ApprovalSrSpv { get; set; } = "Not Started";
        public string ApprovalSectionHead { get; set; } = "Not Started";
        public string ApprovalHC { get; set; } = "Not Started";
        public string SupervisorComments { get; set; } = ""; // Feedback from supervisor
        public string CoacheeId { get; set; } = "";
        public string CoacheeName { get; set; } = "";
    }
}
