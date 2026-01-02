namespace HcPortal.Models
{
    public class CareerCandidate
    {
        public int Id { get; set; }
        public string? Nama { get; set; }
        public string? JabatanSaatIni { get; set; }
        public string? TargetPosisi { get; set; } // Posisi yang dituju
        public string? Unit { get; set; }
        public string? PRL { get; set; }          // Personel Reference Level (Grade)
        public string? Readiness { get; set; }    // "Ready Now", "Ready 1-2 Year", "Retain"
        public int AssessmentScore { get; set; }  // Nilai 0-100
    }
}