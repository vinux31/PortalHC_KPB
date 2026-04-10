namespace HcPortal.Models
{
    /// <summary>
    /// Konfigurasi batas beban coach. Satu baris tunggal di database.
    /// Per D-15/D-16: threshold configurable dan tersimpan di DB.
    /// </summary>
    public class CoachWorkloadThreshold
    {
        public int Id { get; set; }
        /// <summary>Batas maksimal coachee per coach. Default 5.</summary>
        public int MaxCoacheesPerCoach { get; set; } = 5;
        /// <summary>Batas warning (kuning). Default 4.</summary>
        public int WarningThreshold { get; set; } = 4;
        public DateTime UpdatedAt { get; set; }
        public string UpdatedById { get; set; } = "";
    }
}
